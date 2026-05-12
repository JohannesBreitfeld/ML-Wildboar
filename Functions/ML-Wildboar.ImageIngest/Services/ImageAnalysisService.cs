using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ML_Wildboar.ImageIngest.Settings;
using ML_Wildboar.Shared.Storage.Entities;

namespace ML_Wildboar.ImageIngest.Services;

public class ImageAnalysisService : IImageAnalysisService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<ImageAnalysisService> _logger;

    // Static system prompt — cached on first call, reused across invocations within the 5-min TTL.
    private const string SystemPrompt = """
        Du är ett bildanalyssystem för viltkameror placerade i svenska skogar och på åkrar.
        Analysera bilden genom att följa dessa steg i ordning:

        Steg 1 — Sök efter djur
        Granska hela bilden noggrant. Titta efter rörelser, former, ögon, ben, svans eller pälsstrukturer.
        Om inga djur syns: sätt isEmpty = true och detections = [].

        Steg 2 — Räkna individerna
        Om djur syns: räkna hur många individer som är synliga. Observera att bilder nästan alltid
        innehåller individer av SAMMA art — om du ser flera djur antar du att de tillhör samma art
        om det inte finns tydliga bevis för motsatsen.

        Steg 3 — Identifiera art
        Välj art från följande lista (svenska namn):
        - vildsvin
        - rådjur
        - älg
        - räv
        - hare
        - grävling
        - mårdhund
        - lo
        - varg
        - björn
        - fågel  ← använd för ALLA fågelarter oavsett art
        - okänt  ← om du inte kan avgöra arten

        Steg 4 — Bedöm konfidens
        - "hög": djuret är tydligt synligt och arten är lätt att identifiera
        - "medium": djuret syns men ljuset är dåligt eller djuret är delvis dolt
        - "låg": djuret är svårt att se, osäker identifiering

        Steg 5 — Notera väder och beskriv scenen
        Ange väderförhållanden (klart, mulet, regn, dimma, snö, natt) och skriv en kort
        beskrivning av scenen på svenska.

        Returnera ENBART ett JSON-objekt utan markdown-formatering eller annan text:
        {
          "isEmpty": true/false,
          "weather": "...",
          "description": "...",
          "detections": [
            { "species": "...", "count": N, "confidence": "hög|medium|låg" }
          ]
        }
        """;

    public ImageAnalysisService(
        IOptions<AnthropicSettings> settings,
        ILogger<ImageAnalysisService> logger)
    {
        _client = new AnthropicClient { ApiKey = settings.Value.ApiKey };
        _logger = logger;
    }

    public async Task<ImageAnalysisResult> AnalyzeAsync(byte[] imageData)
    {
        var base64Image = Convert.ToBase64String(imageData);

        var delays = new[] { TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120) };
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return await CallClaudeAsync(base64Image);
            }
            catch (Anthropic.Exceptions.AnthropicRateLimitException) when (attempt < delays.Length)
            {
                _logger.LogWarning("Rate limited by Claude API, waiting {delay}s before retry {attempt}",
                    delays[attempt].TotalSeconds, attempt + 1);
                await Task.Delay(delays[attempt]);
            }
        }
    }

    private async Task<ImageAnalysisResult> CallClaudeAsync(string base64Image)
    {
        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = Model.ClaudeSonnet4_6,
            MaxTokens = 1024,
            System = new List<TextBlockParam>
            {
                new()
                {
                    Text = SystemPrompt,
                    CacheControl = new CacheControlEphemeral()
                }
            },
            Messages =
            [
                new MessageParam
                {
                    Role = Role.User,
                    Content = new List<ContentBlockParam>
                    {
                        new ImageBlockParam
                        {
                            Source = new Base64ImageSource
                            {
                                MediaType = "image/jpeg",
                                Data = base64Image
                            }
                        },
                        new TextBlockParam { Text = "Analysera denna bild från en viltkamera." }
                    }
                }
            ]
        });

        _logger.LogInformation(
            "Claude usage — input: {input}, output: {output}, cache_created: {cacheCreated}, cache_read: {cacheRead}",
            response.Usage.InputTokens,
            response.Usage.OutputTokens,
            response.Usage.CacheCreationInputTokens,
            response.Usage.CacheReadInputTokens);

        var rawJson = response.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("Claude returned no text content");

        // Strip markdown code fences if present.
        var json = rawJson.Trim();
        if (json.StartsWith("```"))
        {
            json = json.Substring(json.IndexOf('\n') + 1);
            json = json.Substring(0, json.LastIndexOf("```")).Trim();
        }

        var parsed = JsonSerializer.Deserialize<ClaudeAnalysisResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize Claude response as JSON");

        return new ImageAnalysisResult(
            IsEmpty: parsed.IsEmpty,
            Weather: parsed.Weather ?? string.Empty,
            Description: parsed.Description ?? string.Empty,
            Detections: parsed.Detections ?? [],
            RawJson: json
        );
    }

    private record ClaudeAnalysisResponse(
        [property: JsonPropertyName("isEmpty")] bool IsEmpty,
        [property: JsonPropertyName("weather")] string? Weather,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("detections")] List<AnimalDetection>? Detections
    );
}

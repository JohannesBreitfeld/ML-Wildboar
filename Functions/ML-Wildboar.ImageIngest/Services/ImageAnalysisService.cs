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
    private const string PromptFileRelativePath = "prompts/production.md";

    private readonly AnthropicClient _client;
    private readonly ILogger<ImageAnalysisService> _logger;
    private readonly string _systemPrompt;

    public ImageAnalysisService(
        IOptions<AnthropicSettings> settings,
        ILogger<ImageAnalysisService> logger)
    {
        _client = new AnthropicClient { ApiKey = settings.Value.ApiKey };
        _logger = logger;

        var promptPath = Path.Combine(AppContext.BaseDirectory, PromptFileRelativePath);
        try
        {
            _systemPrompt = File.ReadAllText(promptPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load system prompt from {path}. Letting the exception propagate so the queue message is requeued for retry.", promptPath);
            throw;
        }
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
                    Text = _systemPrompt,
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

        // Extract JSON object in case Claude added any preamble or trailing text.
        if (!json.StartsWith("{"))
        {
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start >= 0 && end > start)
                json = json.Substring(start, end - start + 1);
        }

        var parsed = JsonSerializer.Deserialize<ClaudeAnalysisResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize Claude response as JSON");

        return new ImageAnalysisResult(
            IsEmpty: parsed.IsEmpty,
            TimeOfDay: parsed.TimeOfDay ?? string.Empty,
            Weather: parsed.Weather ?? string.Empty,
            ImageQuality: parsed.ImageQuality ?? string.Empty,
            ContainsHuman: parsed.ContainsHuman,
            ContainsDomestic: parsed.ContainsDomestic,
            ContainsVehicle: parsed.ContainsVehicle,
            Description: parsed.Description ?? string.Empty,
            Detections: parsed.Detections ?? [],
            RawJson: json
        );
    }

    private record ClaudeAnalysisResponse(
        [property: JsonPropertyName("isEmpty")] bool IsEmpty,
        [property: JsonPropertyName("timeOfDay")] string? TimeOfDay,
        [property: JsonPropertyName("weather")] string? Weather,
        [property: JsonPropertyName("imageQuality")] string? ImageQuality,
        [property: JsonPropertyName("containsHuman")] bool ContainsHuman,
        [property: JsonPropertyName("containsDomestic")] bool ContainsDomestic,
        [property: JsonPropertyName("containsVehicle")] bool ContainsVehicle,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("detections")] List<AnimalDetection>? Detections
    );
}

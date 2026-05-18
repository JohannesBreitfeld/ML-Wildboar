using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using ML_Wildboar.PromptEval.Runner.Models;
using ML_Wildboar.Shared.Storage.Entities;

namespace ML_Wildboar.PromptEval.Runner;

// Mirrors the Anthropic call shape used in production (ImageAnalysisService) so eval
// results reflect what the deployed function would produce. Same model, same cache-control,
// same JSON-strip logic. Differs only in:
//   - prompt comes from a file (any version), not the hard-coded production one
//   - retry policy is simpler (one attempt + one retry on rate-limit) for faster eval iteration
internal sealed class PromptRunner
{
    private const string ImageMediaType = "image/jpeg";
    private const string UserText = "Analysera denna bild från en viltkamera.";

    private readonly AnthropicClient _client;
    private readonly IReadOnlyList<ReferenceImage> _referenceImages;

    public PromptRunner(string apiKey, IReadOnlyList<ReferenceImage> referenceImages)
    {
        _client = new AnthropicClient { ApiKey = apiKey };
        _referenceImages = referenceImages;
    }

    public async Task<(AnalysisOutput Output, TokenUsage Usage)> AnalyzeAsync(
        string systemPrompt, byte[] imageBytes, CancellationToken ct = default)
    {
        var base64 = Convert.ToBase64String(imageBytes);

        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return await CallOnceAsync(systemPrompt, base64);
            }
            catch (Anthropic.Exceptions.AnthropicRateLimitException) when (attempt < 1)
            {
                Console.Error.WriteLine("  Rate-limited; waiting 60s before retry.");
                await Task.Delay(TimeSpan.FromSeconds(60), ct);
            }
        }
    }

    private async Task<(AnalysisOutput, TokenUsage)> CallOnceAsync(string systemPrompt, string base64Image)
    {
        var content = new List<ContentBlockParam>();
        for (int i = 0; i < _referenceImages.Count; i++)
        {
            var r = _referenceImages[i];
            content.Add(new ImageBlockParam
            {
                Source = new Base64ImageSource { MediaType = ImageMediaType, Data = Convert.ToBase64String(r.Bytes) }
            });
            // Cache breakpoint on the last reference caption so the whole reference prelude is reused across calls.
            content.Add(i == _referenceImages.Count - 1
                ? new TextBlockParam { Text = r.Caption, CacheControl = new CacheControlEphemeral() }
                : new TextBlockParam { Text = r.Caption });
        }
        content.Add(new ImageBlockParam
        {
            Source = new Base64ImageSource { MediaType = ImageMediaType, Data = base64Image }
        });
        content.Add(new TextBlockParam { Text = UserText });

        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = Model.ClaudeSonnet4_6,
            MaxTokens = 1024,
            System = new List<TextBlockParam>
            {
                new() { Text = systemPrompt, CacheControl = new CacheControlEphemeral() }
            },
            Messages = [new MessageParam { Role = Role.User, Content = content }]
        });

        var usage = new TokenUsage(
            response.Usage.InputTokens,
            response.Usage.OutputTokens,
            response.Usage.CacheCreationInputTokens ?? 0,
            response.Usage.CacheReadInputTokens ?? 0);

        var rawJson = response.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("Claude returned no text content");

        var json = ExtractJsonObject(rawJson);

        var parsed = JsonSerializer.Deserialize<ParsedResponse>(json)
                     ?? throw new InvalidOperationException("Failed to deserialize Claude response as JSON");

        var output = new AnalysisOutput(
            IsEmpty: parsed.IsEmpty,
            TimeOfDay: parsed.TimeOfDay ?? string.Empty,
            Weather: parsed.Weather ?? string.Empty,
            ImageQuality: parsed.ImageQuality ?? string.Empty,
            ContainsHuman: parsed.ContainsHuman,
            ContainsDomestic: parsed.ContainsDomestic,
            ContainsVehicle: parsed.ContainsVehicle,
            Description: parsed.Description ?? string.Empty,
            Detections: parsed.Detections ?? [],
            RawJson: json);

        return (output, usage);
    }

    private static string ExtractJsonObject(string raw)
    {
        var s = raw.Trim();
        if (s.StartsWith("```"))
        {
            s = s[(s.IndexOf('\n') + 1)..];
            s = s[..s.LastIndexOf("```")].Trim();
        }
        if (!s.StartsWith('{'))
        {
            var start = s.IndexOf('{');
            var end = s.LastIndexOf('}');
            if (start >= 0 && end > start)
                s = s.Substring(start, end - start + 1);
        }
        return s;
    }

    private record ParsedResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("isEmpty")] bool IsEmpty,
        [property: System.Text.Json.Serialization.JsonPropertyName("timeOfDay")] string? TimeOfDay,
        [property: System.Text.Json.Serialization.JsonPropertyName("weather")] string? Weather,
        [property: System.Text.Json.Serialization.JsonPropertyName("imageQuality")] string? ImageQuality,
        [property: System.Text.Json.Serialization.JsonPropertyName("containsHuman")] bool ContainsHuman,
        [property: System.Text.Json.Serialization.JsonPropertyName("containsDomestic")] bool ContainsDomestic,
        [property: System.Text.Json.Serialization.JsonPropertyName("containsVehicle")] bool ContainsVehicle,
        [property: System.Text.Json.Serialization.JsonPropertyName("description")] string? Description,
        [property: System.Text.Json.Serialization.JsonPropertyName("detections")] List<AnimalDetection>? Detections
    );
}

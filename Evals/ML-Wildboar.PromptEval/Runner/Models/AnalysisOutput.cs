using System.Text.Json.Serialization;
using ML_Wildboar.Shared.Storage.Entities;

namespace ML_Wildboar.PromptEval.Runner.Models;

public record AnalysisOutput(
    [property: JsonPropertyName("isEmpty")] bool IsEmpty,
    [property: JsonPropertyName("timeOfDay")] string TimeOfDay,
    [property: JsonPropertyName("weather")] string Weather,
    [property: JsonPropertyName("imageQuality")] string ImageQuality,
    [property: JsonPropertyName("containsHuman")] bool ContainsHuman,
    [property: JsonPropertyName("containsDomestic")] bool ContainsDomestic,
    [property: JsonPropertyName("containsVehicle")] bool ContainsVehicle,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("detections")] List<AnimalDetection> Detections,
    [property: JsonPropertyName("rawJson")] string RawJson
);

public record TokenUsage(
    [property: JsonPropertyName("input")] long Input,
    [property: JsonPropertyName("output")] long Output,
    [property: JsonPropertyName("cacheCreated")] long CacheCreated,
    [property: JsonPropertyName("cacheRead")] long CacheRead
);

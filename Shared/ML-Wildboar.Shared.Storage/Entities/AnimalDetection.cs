using System.Text.Json.Serialization;

namespace ML_Wildboar.Shared.Storage.Entities;

/// <summary>
/// Represents a single animal detection result from Claude's image analysis.
/// Deserialised from <see cref="ImageRecord.DetectionsJson"/>.
/// </summary>
public class AnimalDetection
{
    /// <summary>Swedish species name, e.g. "vildsvin", "rådjur", "räv".</summary>
    [JsonPropertyName("species")]
    public string Species { get; set; } = string.Empty;

    /// <summary>Number of individuals of this species visible in the image.</summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>Claude's confidence level: "hög", "medium" or "låg".</summary>
    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = string.Empty;
}

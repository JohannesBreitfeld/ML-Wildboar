using System.Text.Json.Serialization;

namespace ML_Wildboar.Shared.Storage.Entities;

/// <summary>
/// Represents a single animal detection result from Claude's image analysis.
/// Deserialised from <see cref="ImageRecord.DetectionsJson"/>.
/// </summary>
public class AnimalDetection
{
    /// <summary>
    /// Claude's short Swedish justification for the species choice, citing visible
    /// features in the image (silhouette, size, posture, fur etc.). Written before
    /// the species and confidence are committed, so it drives the decision rather
    /// than rationalising it.
    /// </summary>
    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>Swedish species name, e.g. "vildsvin", "rådjur", "räv".</summary>
    [JsonPropertyName("species")]
    public string Species { get; set; } = string.Empty;

    /// <summary>Number of individuals of this species visible in the image (capped at 10).</summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>Claude's confidence in the species identification: "hög", "medel" or "låg".</summary>
    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = string.Empty;
}

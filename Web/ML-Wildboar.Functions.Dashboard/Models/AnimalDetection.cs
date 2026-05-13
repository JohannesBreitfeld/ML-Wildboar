using System.Text.Json.Serialization;

namespace ML_Wildboar.Functions.Dashboard.Models;

public class AnimalDetection
{
    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;

    [JsonPropertyName("species")]
    public string Species { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = string.Empty;
}

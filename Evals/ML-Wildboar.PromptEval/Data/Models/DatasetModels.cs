using System.Text.Json.Serialization;

namespace ML_Wildboar.PromptEval.Data.Models;

public record DatasetManifest(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("items")] List<DatasetItem> Items
);

public record DatasetItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("imagePath")] string ImagePath,
    [property: JsonPropertyName("expected")] ExpectedLabel Expected,
    [property: JsonPropertyName("tags")] List<string>? Tags
);

public record ExpectedLabel(
    [property: JsonPropertyName("isEmpty")] bool IsEmpty,
    [property: JsonPropertyName("detections")] List<ExpectedDetection> Detections,
    [property: JsonPropertyName("notes")] string? Notes
);

public record ExpectedDetection(
    [property: JsonPropertyName("species")] string Species,
    [property: JsonPropertyName("count")] int Count
);

public record LoadedDataset(
    DatasetManifest Manifest,
    string ManifestPath,
    Dictionary<string, string> ResolvedImagePaths
);

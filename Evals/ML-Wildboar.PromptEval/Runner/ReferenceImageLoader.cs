using System.Text.Json;
using System.Text.Json.Serialization;

namespace ML_Wildboar.PromptEval.Runner;

internal sealed record ReferenceImage(string Id, byte[] Bytes, string Caption);

internal static class ReferenceImageLoader
{
    // Loads prompts/reference-images/manifest.json + all referenced JPGs.
    // Returns empty list if the folder or manifest doesn't exist — the prompt then runs
    // without reference images, matching the pre-feature behaviour.
    public static IReadOnlyList<ReferenceImage> Load(string promptsRoot)
    {
        var dir = Path.Combine(promptsRoot, "reference-images");
        var manifestPath = Path.Combine(dir, "manifest.json");
        if (!File.Exists(manifestPath)) return [];

        var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath), JsonOptions)
                       ?? throw new InvalidOperationException($"Failed to parse {manifestPath}");

        var loaded = new List<ReferenceImage>();
        foreach (var item in manifest.Items)
        {
            var imagePath = Path.Combine(dir, item.ImagePath);
            if (!File.Exists(imagePath))
                throw new FileNotFoundException($"Reference image not found: {imagePath} (referenced by {manifestPath})");
            loaded.Add(new ReferenceImage(item.Id, File.ReadAllBytes(imagePath), item.Caption));
        }
        return loaded;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
    };

    private record Manifest(
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("items")] List<ManifestItem> Items);

    private record ManifestItem(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("imagePath")] string ImagePath,
        [property: JsonPropertyName("caption")] string Caption);
}

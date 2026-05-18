using System.Text.Json;
using System.Text.Json.Serialization;

namespace ML_Wildboar.ImageIngest.Services;

internal sealed record ReferenceImage(string Id, byte[] Bytes, string Caption);

internal static class ReferenceImageLoader
{
    // Loads prompts/reference-images/manifest.json + all referenced JPGs from the function's
    // output directory. Returns empty list if the folder or manifest doesn't exist — the
    // service then runs without reference images.
    public static IReadOnlyList<ReferenceImage> Load(string baseDirectory)
    {
        var dir = Path.Combine(baseDirectory, "prompts", "reference-images");
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

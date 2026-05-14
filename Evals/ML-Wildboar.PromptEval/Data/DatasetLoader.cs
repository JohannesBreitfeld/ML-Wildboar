using System.Text.Json;
using ML_Wildboar.PromptEval.Data.Models;

namespace ML_Wildboar.PromptEval.Data;

internal static class DatasetLoader
{
    public static LoadedDataset Load(string repoRoot, string datasetName)
    {
        var datasetDir = Path.Combine(repoRoot, "Evals", "datasets", datasetName);
        var manifestPath = Path.Combine(datasetDir, "dataset.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException($"Dataset manifest not found: {manifestPath}");

        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<DatasetManifest>(json, JsonOptions)
                       ?? throw new InvalidOperationException($"Failed to parse dataset manifest: {manifestPath}");

        var resolved = new Dictionary<string, string>();
        var missing = new List<string>();
        foreach (var item in manifest.Items)
        {
            var path = Path.IsPathRooted(item.ImagePath)
                ? item.ImagePath
                : Path.GetFullPath(Path.Combine(datasetDir, item.ImagePath));
            if (!File.Exists(path)) missing.Add($"  {item.Id} → {path}");
            resolved[item.Id] = path;
        }

        if (missing.Count > 0)
        {
            throw new FileNotFoundException(
                $"{missing.Count} image(s) referenced by dataset '{datasetName}' are missing on disk. " +
                "Images are gitignored — make sure your local images/ folder is populated:\n" +
                string.Join('\n', missing));
        }

        return new LoadedDataset(manifest, manifestPath, resolved);
    }

    public static List<string> ListDatasetNames(string repoRoot)
    {
        var dir = Path.Combine(repoRoot, "Evals", "datasets");
        if (!Directory.Exists(dir)) return [];
        return Directory.EnumerateDirectories(dir)
            .Where(d => File.Exists(Path.Combine(d, "dataset.json")))
            .Select(d => Path.GetFileName(d)!)
            .OrderBy(n => n)
            .ToList();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
    };
}

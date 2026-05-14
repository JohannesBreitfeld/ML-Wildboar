using System.Text.Json;
using ML_Wildboar.PromptEval.Evaluation.Models;

namespace ML_Wildboar.PromptEval.Reporting;

internal static class JsonReporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static void WriteSummary(string runDir, RunSummary summary)
    {
        var path = Path.Combine(runDir, "summary.json");
        File.WriteAllText(path, JsonSerializer.Serialize(summary, Options));
    }

    public static void WriteResults(string runDir, RunArtifact artifact)
    {
        var path = Path.Combine(runDir, "results.json");
        File.WriteAllText(path, JsonSerializer.Serialize(artifact, Options));
    }

    public static RunArtifact ReadArtifact(string runDir)
    {
        var resultsPath = Path.Combine(runDir, "results.json");
        if (!File.Exists(resultsPath))
            throw new FileNotFoundException($"results.json not found in run directory: {runDir}");
        var json = File.ReadAllText(resultsPath);
        return JsonSerializer.Deserialize<RunArtifact>(json, Options)
            ?? throw new InvalidOperationException($"Failed to parse {resultsPath}");
    }
}

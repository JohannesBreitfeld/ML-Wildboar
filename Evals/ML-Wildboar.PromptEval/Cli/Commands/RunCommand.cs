using System.Diagnostics;
using Anthropic.Models.Messages;
using ML_Wildboar.PromptEval.Data;
using ML_Wildboar.PromptEval.Evaluation;
using ML_Wildboar.PromptEval.Evaluation.Models;
using ML_Wildboar.PromptEval.Reporting;
using ML_Wildboar.PromptEval.Runner;

namespace ML_Wildboar.PromptEval.Cli.Commands;

internal sealed class RunCommand(string repoRoot)
{
    public async Task<int> ExecuteAsync(string[] args)
    {
        var promptName = CliArgs.RequireFlag(args, "--prompt");
        var datasetName = CliArgs.RequireFlag(args, "--dataset");
        var label = CliArgs.Flag(args, "--label");
        var modelArg = CliArgs.Flag(args, "--model") ?? "sonnet";
        var model = modelArg.ToLowerInvariant() switch
        {
            "sonnet" or "sonnet-4-6" => Model.ClaudeSonnet4_6,
            "opus" or "opus-4-7" => Model.ClaudeOpus4_7,
            "haiku" or "haiku-4-5" => Model.ClaudeHaiku4_5,
            _ => throw new ArgumentException($"Unknown --model value: {modelArg}. Use sonnet, opus, or haiku.")
        };
        var apiKey = CliArgs.Flag(args, "--api-key")
                     ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
                     ?? throw new ArgumentException("Missing API key. Set ANTHROPIC_API_KEY or pass --api-key.");

        var promptPath = Path.Combine(repoRoot, "prompts", $"{promptName}.md");
        if (!File.Exists(promptPath))
            throw new FileNotFoundException($"Prompt not found: {promptPath}");
        var systemPrompt = File.ReadAllText(promptPath);

        var dataset = DatasetLoader.Load(repoRoot, datasetName);

        var runId = BuildRunId(promptName, datasetName, label);
        var runDir = Path.Combine(repoRoot, "Evals", "runs", runId);
        Directory.CreateDirectory(runDir);

        Console.WriteLine($"Run: {runId}");
        Console.WriteLine($"  Prompt:  {promptPath}");
        Console.WriteLine($"  Model:   {modelArg} ({model})");
        Console.WriteLine($"  Dataset: {dataset.Manifest.Name} ({dataset.Manifest.Items.Count} items)");
        Console.WriteLine();

        var refs = ReferenceImageLoader.Load(Path.Combine(repoRoot, "prompts"));
        if (refs.Count > 0)
            Console.WriteLine($"  References: {refs.Count} image(s) — {string.Join(", ", refs.Select(r => r.Id))}");

        var runner = new PromptRunner(apiKey, refs, model);
        var startedAt = DateTimeOffset.UtcNow;
        var results = new List<ImageRunResult>();

        foreach (var item in dataset.Manifest.Items)
        {
            var imagePath = dataset.ResolvedImagePaths[item.Id];
            Console.Write($"  {item.Id} ... ");
            var sw = Stopwatch.StartNew();
            ImageRunResult result;
            try
            {
                var bytes = await File.ReadAllBytesAsync(imagePath);
                var (output, usage) = await runner.AnalyzeAsync(systemPrompt, bytes);
                var score = Evaluator.Score(item, output);
                sw.Stop();
                result = new ImageRunResult(
                    ItemId: item.Id,
                    ImagePath: item.ImagePath,
                    Output: output,
                    Usage: usage,
                    Error: null,
                    DurationMs: sw.ElapsedMilliseconds,
                    Score: score);
                Console.WriteLine($"{(score.IsCorrect ? "✓" : "✗")} ({score.EmptyClass}, {sw.ElapsedMilliseconds}ms)");
            }
            catch (Exception ex)
            {
                sw.Stop();
                result = new ImageRunResult(
                    ItemId: item.Id,
                    ImagePath: item.ImagePath,
                    Output: null,
                    Usage: null,
                    Error: ex.Message,
                    DurationMs: sw.ElapsedMilliseconds,
                    Score: null);
                Console.WriteLine($"💥 {ex.Message}");
            }
            results.Add(result);
        }

        var summary = Aggregator.Summarise(runId, promptName, datasetName, startedAt, results);
        var artifact = new RunArtifact(summary, results);

        JsonReporter.WriteSummary(runDir, summary);
        JsonReporter.WriteResults(runDir, artifact);
        MarkdownReporter.Write(runDir, artifact);

        Console.WriteLine();
        Console.WriteLine($"Written to: {runDir}");
        Console.WriteLine($"  - report.md");
        Console.WriteLine($"  - summary.json");
        Console.WriteLine($"  - results.json");
        Console.WriteLine();
        Console.WriteLine($"Empty/animal: TP={summary.AnimalTruePositives} TN={summary.AnimalTrueNegatives} FP={summary.AnimalFalsePositives} FN={summary.AnimalFalseNegatives}");
        Console.WriteLine($"Correct images: {summary.CorrectImages}/{summary.Succeeded}");
        return 0;
    }

    private static string BuildRunId(string promptName, string datasetName, string? label)
    {
        var ts = DateTime.UtcNow.ToString("yyyy-MM-dd_HHmm");
        var suffix = string.IsNullOrEmpty(label) ? "" : $"_{label}";
        return $"{ts}_{promptName}_{datasetName}{suffix}";
    }
}

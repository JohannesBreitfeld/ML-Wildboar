using System.Text;
using ML_Wildboar.PromptEval.Evaluation.Models;
using ML_Wildboar.PromptEval.Reporting;

namespace ML_Wildboar.PromptEval.Cli.Commands;

internal sealed class DiffCommand(string repoRoot)
{
    public int Execute(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: eval diff <run-a> <run-b>");
            return 2;
        }

        var aPath = Path.Combine(repoRoot, "Evals", "runs", args[0]);
        var bPath = Path.Combine(repoRoot, "Evals", "runs", args[1]);
        var a = JsonReporter.ReadArtifact(aPath);
        var b = JsonReporter.ReadArtifact(bPath);

        Console.Write(Render(a, b));
        return 0;
    }

    public static string Render(RunArtifact a, RunArtifact b)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Diff: `{a.Summary.RunId}` → `{b.Summary.RunId}`");
        sb.AppendLine();
        sb.AppendLine($"Prompt: {a.Summary.PromptName} → {b.Summary.PromptName}");
        sb.AppendLine($"Dataset: {a.Summary.DatasetName} → {b.Summary.DatasetName}");
        sb.AppendLine();

        var aById = a.Results.ToDictionary(r => r.ItemId);
        var bById = b.Results.ToDictionary(r => r.ItemId);

        var commonIds = aById.Keys.Intersect(bById.Keys).OrderBy(id => id).ToList();
        var aOnly = aById.Keys.Except(bById.Keys).ToList();
        var bOnly = bById.Keys.Except(aById.Keys).ToList();

        int improved = 0, regressed = 0, sameCorrect = 0, sameWrong = 0;
        var regressions = new List<(string Id, ImageRunResult A, ImageRunResult B)>();
        var improvements = new List<(string Id, ImageRunResult A, ImageRunResult B)>();

        foreach (var id in commonIds)
        {
            var ra = aById[id];
            var rb = bById[id];
            var aOk = ra.Score?.IsCorrect ?? false;
            var bOk = rb.Score?.IsCorrect ?? false;
            if (!aOk && bOk) { improved++; improvements.Add((id, ra, rb)); }
            else if (aOk && !bOk) { regressed++; regressions.Add((id, ra, rb)); }
            else if (aOk && bOk) sameCorrect++;
            else sameWrong++;
        }

        sb.AppendLine("## Summary (species-correct)");
        sb.AppendLine();
        sb.AppendLine($"- Images in both runs: {commonIds.Count}");
        sb.AppendLine($"- ✓ → ✓ unchanged-correct:   {sameCorrect}");
        sb.AppendLine($"- ✗ → ✓ improved:            {improved}");
        sb.AppendLine($"- ✓ → ✗ **regressed**:       {regressed}");
        sb.AppendLine($"- ✗ → ✗ unchanged-wrong:     {sameWrong}");
        if (aOnly.Count > 0) sb.AppendLine($"- Only in {a.Summary.RunId}: {aOnly.Count}");
        if (bOnly.Count > 0) sb.AppendLine($"- Only in {b.Summary.RunId}: {bOnly.Count}");
        sb.AppendLine();

        // Parallel summary for count-accurate (stricter).
        int countImproved = 0, countRegressed = 0;
        foreach (var id in commonIds)
        {
            var aCount = aById[id].Score?.CountCorrect ?? false;
            var bCount = bById[id].Score?.CountCorrect ?? false;
            if (!aCount && bCount) countImproved++;
            else if (aCount && !bCount) countRegressed++;
        }
        sb.AppendLine($"## Summary (count-accurate, stricter)");
        sb.AppendLine();
        sb.AppendLine($"- A: {a.Summary.CountAccurateImages}/{a.Summary.Succeeded} → B: {b.Summary.CountAccurateImages}/{b.Summary.Succeeded}");
        sb.AppendLine($"- ✗ → ✓ count-improved:      {countImproved}");
        sb.AppendLine($"- ✓ → ✗ count-**regressed**: {countRegressed}");
        sb.AppendLine();

        sb.AppendLine("## Confusion-matrix delta");
        sb.AppendLine();
        sb.AppendLine($"|         |  A  |  B  | Δ |");
        sb.AppendLine($"|---------|----:|----:|--:|");
        sb.AppendLine(Row("TP (correct hit)",      a.Summary.AnimalTruePositives,  b.Summary.AnimalTruePositives));
        sb.AppendLine(Row("TN (correct empty)",    a.Summary.AnimalTrueNegatives,  b.Summary.AnimalTrueNegatives));
        sb.AppendLine(Row("FP (fabricated)",       a.Summary.AnimalFalsePositives, b.Summary.AnimalFalsePositives));
        sb.AppendLine(Row("FN (missed)",           a.Summary.AnimalFalseNegatives, b.Summary.AnimalFalseNegatives));
        sb.AppendLine();

        if (regressed > 0)
        {
            sb.AppendLine("## Regressions (✓ → ✗)");
            sb.AppendLine();
            foreach (var (id, ra, rb) in regressions)
            {
                sb.AppendLine($"- **{id}**: was {ra.Score?.EmptyClass} → now {rb.Score?.EmptyClass}");
            }
            sb.AppendLine();
        }
        if (improved > 0)
        {
            sb.AppendLine("## Improvements (✗ → ✓)");
            sb.AppendLine();
            foreach (var (id, ra, rb) in improvements)
            {
                sb.AppendLine($"- **{id}**: was {ra.Score?.EmptyClass} → now {rb.Score?.EmptyClass}");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string Row(string label, int aVal, int bVal)
    {
        var delta = bVal - aVal;
        var sign = delta > 0 ? $"+{delta}" : delta.ToString();
        return $"| {label,-22} | {aVal,3} | {bVal,3} | {sign,3} |";
    }
}

using System.Globalization;
using System.Text;
using ML_Wildboar.PromptEval.Evaluation.Models;

namespace ML_Wildboar.PromptEval.Reporting;

internal static class MarkdownReporter
{
    public static void Write(string runDir, RunArtifact artifact)
    {
        var path = Path.Combine(runDir, "report.md");
        File.WriteAllText(path, Render(artifact));
    }

    public static string Render(RunArtifact artifact)
    {
        var s = artifact.Summary;
        var sb = new StringBuilder();
        var ci = CultureInfo.InvariantCulture;

        sb.AppendLine($"# Eval run: `{s.RunId}`");
        sb.AppendLine();
        sb.AppendLine($"- Prompt: **{s.PromptName}**");
        sb.AppendLine($"- Dataset: **{s.DatasetName}**");
        sb.AppendLine($"- Started: {s.StartedAt:O}");
        sb.AppendLine($"- Items: {s.TotalItems} ({s.Succeeded} succeeded, {s.Failed} failed)");
        sb.AppendLine();

        sb.AppendLine("## Image-level animal-presence confusion matrix");
        sb.AppendLine();
        sb.AppendLine("|                    | Animal present (expected) | Empty (expected) |");
        sb.AppendLine("|--------------------|---------------------------|------------------|");
        sb.AppendLine($"| Claude: animals    | TP {s.AnimalTruePositives,3}                    | **FP {s.AnimalFalsePositives,3}** (fabricated) |");
        sb.AppendLine($"| Claude: empty      | **FN {s.AnimalFalseNegatives,3}** (missed)        | TN {s.AnimalTrueNegatives,3}           |");
        sb.AppendLine();
        var classified = s.AnimalTruePositives + s.AnimalTrueNegatives + s.AnimalFalsePositives + s.AnimalFalseNegatives;
        if (classified > 0)
        {
            var emptyAcc = (double)(s.AnimalTruePositives + s.AnimalTrueNegatives) / classified;
            sb.AppendLine($"Empty/animal accuracy: **{emptyAcc:P1}**.");
            sb.AppendLine();
            sb.AppendLine($"- **Species-correct images:** {s.CorrectImages}/{s.Succeeded} — empty/animal call right and no species missed or fabricated. Count can differ.");
            sb.AppendLine($"- **Count-accurate images:** {s.CountAccurateImages}/{s.Succeeded} — also exact-count match on every species. Stricter signal for ranking prompts.");
            sb.AppendLine();
        }

        sb.AppendLine("## Per-species");
        sb.AppendLine();
        sb.AppendLine("| Species  |  TP |  FP |  FN | Precision | Recall |   F1 |");
        sb.AppendLine("|----------|----:|----:|----:|----------:|-------:|-----:|");
        foreach (var sp in s.PerSpecies)
        {
            sb.AppendLine($"| {sp.Species,-8} | {sp.TruePositives,3} | {sp.FalsePositives,3} | {sp.FalseNegatives,3} | {sp.Precision.ToString("F2", ci),9} | {sp.Recall.ToString("F2", ci),6} | {sp.F1.ToString("F2", ci),4} |");
        }
        sb.AppendLine();

        if (s.PerTag.Count > 0)
        {
            sb.AppendLine("## Per-tag breakdown");
            sb.AppendLine();
            sb.AppendLine("Each row totals every image carrying the tag. Camera names (lower/upper/gattet) and scene/difficulty tags appear side by side so you can spot if a prompt change helps one camera at the cost of another.");
            sb.AppendLine();
            sb.AppendLine("| Tag                  |   N | Spec. correct | Count exact |  TP |  TN |  FP |  FN |");
            sb.AppendLine("|----------------------|----:|--------------:|------------:|----:|----:|----:|----:|");
            foreach (var t in s.PerTag)
            {
                var spec = $"{t.SpeciesCorrect}/{t.Images}";
                var count = $"{t.CountAccurate}/{t.Images}";
                sb.AppendLine(
                    $"| {t.Tag,-20} | {t.Images,3} | {spec,13} | {count,11} | {t.AnimalTruePositives,3} | {t.AnimalTrueNegatives,3} | {t.AnimalFalsePositives,3} | {t.AnimalFalseNegatives,3} |");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## Token usage");
        sb.AppendLine();
        sb.AppendLine($"- Input: {s.TotalInputTokens:N0}");
        sb.AppendLine($"- Output: {s.TotalOutputTokens:N0}");
        sb.AppendLine($"- Cache created: {s.TotalCacheCreatedTokens:N0}");
        sb.AppendLine($"- Cache read: {s.TotalCacheReadTokens:N0}");
        sb.AppendLine();

        sb.AppendLine("## Per-image results");
        sb.AppendLine();
        sb.AppendLine("| Status | Item | Empty | Species deltas | Time |");
        sb.AppendLine("|--------|------|-------|----------------|------|");
        foreach (var r in artifact.Results)
        {
            var icon = StatusIcon(r);
            var deltas = r.Score is null ? "—" : FormatDeltas(r.Score);
            var emptyTag = r.Score?.EmptyClass.ToString() ?? "—";
            sb.AppendLine($"| {icon} | {r.ItemId} | {emptyTag} | {deltas} | {r.DurationMs}ms |");
        }
        sb.AppendLine();

        var failing = artifact.Results
            .Where(r => r.Error is not null || (r.Score is { IsCorrect: false }))
            .ToList();
        if (failing.Count > 0)
        {
            sb.AppendLine("## Failing cases");
            sb.AppendLine();
            foreach (var r in failing)
            {
                sb.AppendLine($"### {r.ItemId}");
                sb.AppendLine();
                if (r.Error is not null)
                {
                    sb.AppendLine($"**Error:** {r.Error}");
                    sb.AppendLine();
                    continue;
                }
                if (r.Score is { } sc)
                {
                    sb.AppendLine($"- Empty classification: **{sc.EmptyClass}**");
                    if (sc.Notes is not null) sb.AppendLine($"- Label notes: {sc.Notes}");
                    foreach (var e in sc.SpeciesErrors)
                    {
                        var tag = e.IsFabricated ? "**fabricated**" : e.IsMissed ? "**missed**" : "delta";
                        sb.AppendLine($"- {e.Species}: expected {e.Expected}, got {e.Actual} ({tag})");
                    }
                }
                if (r.Output is { } o)
                {
                    sb.AppendLine();
                    sb.AppendLine($"**Claude description:** {o.Description}");
                    if (o.Detections.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("Claude detections:");
                        foreach (var d in o.Detections)
                        {
                            sb.AppendLine($"  - {d.Species} ×{d.Count} ({d.Confidence}): {d.Reasoning}");
                        }
                    }
                }
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    // ✓ = species correct AND count correct (best)
    // ~ = species correct but at least one count off (acceptable but not exact)
    // ✗ = species mismatch (missed or fabricated)
    // 💥 = run error
    private static string StatusIcon(ImageRunResult r)
    {
        if (r.Error is not null) return "💥";
        if (r.Score is null) return "?";
        if (r.Score.CountCorrect) return "✓";
        return r.Score.IsCorrect ? "~" : "✗";
    }

    private static string FormatDeltas(ImageScore sc)
    {
        if (sc.SpeciesErrors.Count == 0) return "—";
        return string.Join(", ",
            sc.SpeciesErrors.Select(e =>
                e.Delta == 0 ? $"{e.Species}={e.Actual}" : $"{e.Species} {e.Expected}→{e.Actual}"));
    }
}

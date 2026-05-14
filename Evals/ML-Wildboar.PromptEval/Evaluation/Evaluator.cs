using ML_Wildboar.PromptEval.Data.Models;
using ML_Wildboar.PromptEval.Evaluation.Models;
using ML_Wildboar.PromptEval.Runner.Models;

namespace ML_Wildboar.PromptEval.Evaluation;

internal static class Evaluator
{
    public static ImageScore Score(DatasetItem item, AnalysisOutput output)
    {
        var expected = item.Expected;

        var emptyClass = (expected.IsEmpty, output.IsEmpty) switch
        {
            (true, true)   => EmptyClassification.TrueNegative,    // empty, said empty
            (false, false) => EmptyClassification.TruePositive,    // animals present, detected
            (true, false)  => EmptyClassification.FalsePositive,   // empty, but reported animals (fabricated)
            (false, true)  => EmptyClassification.FalseNegative,   // animals present, said empty (missed)
        };

        // Compare species counts. Aggregate Claude's detections per species first
        // (multiple detection objects for same species → sum counts).
        var actualCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in output.Detections)
        {
            var key = (d.Species ?? "").Trim().ToLowerInvariant();
            if (key.Length == 0) continue;
            actualCounts[key] = actualCounts.GetValueOrDefault(key) + d.Count;
        }

        var expectedCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in expected.Detections)
        {
            var key = d.Species.Trim().ToLowerInvariant();
            if (key.Length == 0) continue;
            expectedCounts[key] = expectedCounts.GetValueOrDefault(key) + d.Count;
        }

        var allSpecies = expectedCounts.Keys.Union(actualCounts.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(s => s);
        var errors = new List<SpeciesError>();
        foreach (var species in allSpecies)
        {
            var exp = expectedCounts.GetValueOrDefault(species);
            var act = actualCounts.GetValueOrDefault(species);
            errors.Add(new SpeciesError(species, exp, act));
        }

        // Species-correct image: empty/animal call right AND no species missed or fabricated.
        // Count differences are recorded but do NOT fail IsCorrect.
        var hasSpeciesSetMismatch = errors.Any(e => e.IsMissed || e.IsFabricated);
        var emptyCorrect = emptyClass is EmptyClassification.TruePositive or EmptyClassification.TrueNegative;
        var isCorrect = emptyCorrect && !hasSpeciesSetMismatch;

        // Count-accurate image: also requires every species count to match exactly.
        var countCorrect = isCorrect && errors.All(e => e.Delta == 0);

        return new ImageScore(
            ItemId: item.Id,
            ImagePath: item.ImagePath,
            Tags: item.Tags,
            EmptyClass: emptyClass,
            SpeciesErrors: errors,
            IsCorrect: isCorrect,
            CountCorrect: countCorrect,
            Notes: expected.Notes);
    }
}

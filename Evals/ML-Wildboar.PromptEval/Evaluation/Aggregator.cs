using ML_Wildboar.PromptEval.Evaluation.Models;

namespace ML_Wildboar.PromptEval.Evaluation;

internal static class Aggregator
{
    public static RunSummary Summarise(
        string runId, string promptName, string datasetName, DateTimeOffset startedAt,
        IReadOnlyList<ImageRunResult> results)
    {
        int succeeded = 0, failed = 0, correctImages = 0, countAccurateImages = 0;
        int aTp = 0, aTn = 0, aFp = 0, aFn = 0;
        long inTokens = 0, outTokens = 0, cacheCreated = 0, cacheRead = 0;

        // species → (tp, fp, fn)
        // tp = species in both expected and actual; fp = in actual only; fn = in expected only.
        var speciesStats = new Dictionary<string, (int Tp, int Fp, int Fn)>(StringComparer.OrdinalIgnoreCase);

        // tag → mutable bucket. One item with N tags contributes to N buckets.
        var tagStats = new Dictionary<string, TagBucket>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in results)
        {
            if (r.Error is not null) { failed++; continue; }
            succeeded++;

            if (r.Usage is { } u)
            {
                inTokens += u.Input;
                outTokens += u.Output;
                cacheCreated += u.CacheCreated;
                cacheRead += u.CacheRead;
            }

            if (r.Score is null) continue;
            if (r.Score.IsCorrect) correctImages++;
            if (r.Score.CountCorrect) countAccurateImages++;

            // Increment per-tag buckets for every tag attached to this item.
            if (r.Score.Tags is { } tags)
            {
                foreach (var tag in tags)
                {
                    if (!tagStats.TryGetValue(tag, out var bucket))
                    {
                        bucket = new TagBucket();
                        tagStats[tag] = bucket;
                    }
                    bucket.Images++;
                    if (r.Score.IsCorrect) bucket.SpeciesCorrect++;
                    if (r.Score.CountCorrect) bucket.CountAccurate++;
                    switch (r.Score.EmptyClass)
                    {
                        case EmptyClassification.TruePositive:  bucket.Tp++; break;
                        case EmptyClassification.TrueNegative:  bucket.Tn++; break;
                        case EmptyClassification.FalsePositive: bucket.Fp++; break;
                        case EmptyClassification.FalseNegative: bucket.Fn++; break;
                    }
                }
            }

            switch (r.Score.EmptyClass)
            {
                case EmptyClassification.TruePositive:  aTp++; break;
                case EmptyClassification.TrueNegative:  aTn++; break;
                case EmptyClassification.FalsePositive: aFp++; break;
                case EmptyClassification.FalseNegative: aFn++; break;
            }

            foreach (var e in r.Score.SpeciesErrors)
            {
                var (tp, fp, fn) = speciesStats.GetValueOrDefault(e.Species);
                if (e.Expected > 0 && e.Actual > 0) tp++;
                else if (e.Expected == 0 && e.Actual > 0) fp++;
                else if (e.Expected > 0 && e.Actual == 0) fn++;
                speciesStats[e.Species] = (tp, fp, fn);
            }
        }

        var perTag = tagStats
            .Select(kv => new TagStat(
                Tag: kv.Key,
                Images: kv.Value.Images,
                SpeciesCorrect: kv.Value.SpeciesCorrect,
                CountAccurate: kv.Value.CountAccurate,
                AnimalTruePositives: kv.Value.Tp,
                AnimalTrueNegatives: kv.Value.Tn,
                AnimalFalsePositives: kv.Value.Fp,
                AnimalFalseNegatives: kv.Value.Fn))
            .OrderBy(t => t.Tag)
            .ToList();

        var perSpecies = speciesStats
            .Select(kv =>
            {
                var (tp, fp, fn) = kv.Value;
                var precision = tp + fp == 0 ? 0 : (double)tp / (tp + fp);
                var recall    = tp + fn == 0 ? 0 : (double)tp / (tp + fn);
                var f1        = precision + recall == 0 ? 0 : 2 * precision * recall / (precision + recall);
                return new SpeciesAggregate(kv.Key, tp, fp, fn, precision, recall, f1);
            })
            .OrderBy(s => s.Species)
            .ToList();

        return new RunSummary(
            RunId: runId,
            PromptName: promptName,
            DatasetName: datasetName,
            StartedAt: startedAt,
            TotalItems: results.Count,
            Succeeded: succeeded,
            Failed: failed,
            CorrectImages: correctImages,
            CountAccurateImages: countAccurateImages,
            AnimalTruePositives: aTp,
            AnimalTrueNegatives: aTn,
            AnimalFalsePositives: aFp,
            AnimalFalseNegatives: aFn,
            PerSpecies: perSpecies,
            PerTag: perTag,
            TotalInputTokens: inTokens,
            TotalOutputTokens: outTokens,
            TotalCacheCreatedTokens: cacheCreated,
            TotalCacheReadTokens: cacheRead);
    }

    private sealed class TagBucket
    {
        public int Images;
        public int SpeciesCorrect;
        public int CountAccurate;
        public int Tp;
        public int Tn;
        public int Fp;
        public int Fn;
    }
}

using System.Text.Json.Serialization;
using ML_Wildboar.PromptEval.Runner.Models;

namespace ML_Wildboar.PromptEval.Evaluation.Models;

// Positive class = "animal present" (what we're trying to detect).
public enum EmptyClassification
{
    TruePositive,    // animal present and Claude detected an animal — correct hit
    TrueNegative,    // image is empty and Claude said empty — correct rejection
    FalsePositive,   // image is empty but Claude reported an animal — fabricated detection
    FalseNegative,   // animal present but Claude said empty — missed animal
}

public record SpeciesError(
    [property: JsonPropertyName("species")] string Species,
    [property: JsonPropertyName("expected")] int Expected,
    [property: JsonPropertyName("actual")] int Actual
)
{
    public int Delta => Actual - Expected;
    public bool IsMissed => Expected > 0 && Actual == 0;
    public bool IsFabricated => Expected == 0 && Actual > 0;
}

public record ImageScore(
    [property: JsonPropertyName("itemId")] string ItemId,
    [property: JsonPropertyName("imagePath")] string ImagePath,
    [property: JsonPropertyName("tags")] List<string>? Tags,
    [property: JsonPropertyName("emptyClass")] EmptyClassification EmptyClass,
    [property: JsonPropertyName("speciesErrors")] List<SpeciesError> SpeciesErrors,
    // IsCorrect = empty/animal call correct AND species set matches (no fabricated or missed species).
    // Count differences are recorded but do NOT fail IsCorrect — a species-correct result is acceptable.
    [property: JsonPropertyName("isCorrect")] bool IsCorrect,
    // CountCorrect = IsCorrect AND every species count matches exactly. The stricter signal for ranking prompts.
    [property: JsonPropertyName("countCorrect")] bool CountCorrect,
    [property: JsonPropertyName("notes")] string? Notes
);

public record ImageRunResult(
    [property: JsonPropertyName("itemId")] string ItemId,
    [property: JsonPropertyName("imagePath")] string ImagePath,
    [property: JsonPropertyName("output")] AnalysisOutput? Output,
    [property: JsonPropertyName("usage")] TokenUsage? Usage,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("durationMs")] long DurationMs,
    [property: JsonPropertyName("score")] ImageScore? Score
);

// Per-tag aggregation. A tag is anything attached to an item in dataset.json — could be
// a camera name (lower/upper/gattet), a difficulty marker (hard, fn-trap), a scene type
// (rocks-plus-animal, ogonreflex), etc. Per-tag stats answer questions like "did this prompt
// help the upper camera at the cost of regressing the lower camera?".
public record TagStat(
    [property: JsonPropertyName("tag")] string Tag,
    [property: JsonPropertyName("images")] int Images,
    [property: JsonPropertyName("speciesCorrect")] int SpeciesCorrect,
    [property: JsonPropertyName("countAccurate")] int CountAccurate,
    [property: JsonPropertyName("animalTp")] int AnimalTruePositives,
    [property: JsonPropertyName("animalTn")] int AnimalTrueNegatives,
    [property: JsonPropertyName("animalFp")] int AnimalFalsePositives,
    [property: JsonPropertyName("animalFn")] int AnimalFalseNegatives
);

public record SpeciesAggregate(
    [property: JsonPropertyName("species")] string Species,
    [property: JsonPropertyName("tp")] int TruePositives,
    [property: JsonPropertyName("fp")] int FalsePositives,
    [property: JsonPropertyName("fn")] int FalseNegatives,
    [property: JsonPropertyName("precision")] double Precision,
    [property: JsonPropertyName("recall")] double Recall,
    [property: JsonPropertyName("f1")] double F1
);

public record RunSummary(
    [property: JsonPropertyName("runId")] string RunId,
    [property: JsonPropertyName("promptName")] string PromptName,
    [property: JsonPropertyName("datasetName")] string DatasetName,
    [property: JsonPropertyName("startedAt")] DateTimeOffset StartedAt,
    [property: JsonPropertyName("totalItems")] int TotalItems,
    [property: JsonPropertyName("succeeded")] int Succeeded,
    [property: JsonPropertyName("failed")] int Failed,
    // Species set matches expected (counts can differ).
    [property: JsonPropertyName("correctImages")] int CorrectImages,
    // Stricter: species set matches AND every count matches exactly.
    [property: JsonPropertyName("countAccurateImages")] int CountAccurateImages,
    // Image-level animal-presence confusion matrix (positive class = animal present).
    [property: JsonPropertyName("animalTp")] int AnimalTruePositives,    // animal present, Claude detected
    [property: JsonPropertyName("animalTn")] int AnimalTrueNegatives,    // empty, Claude empty
    [property: JsonPropertyName("animalFp")] int AnimalFalsePositives,   // empty, Claude detected (fabricated)
    [property: JsonPropertyName("animalFn")] int AnimalFalseNegatives,   // animal present, Claude empty (missed)
    [property: JsonPropertyName("perSpecies")] List<SpeciesAggregate> PerSpecies,
    [property: JsonPropertyName("perTag")] List<TagStat> PerTag,
    [property: JsonPropertyName("totalInputTokens")] long TotalInputTokens,
    [property: JsonPropertyName("totalOutputTokens")] long TotalOutputTokens,
    [property: JsonPropertyName("totalCacheCreatedTokens")] long TotalCacheCreatedTokens,
    [property: JsonPropertyName("totalCacheReadTokens")] long TotalCacheReadTokens
);

public record RunArtifact(
    [property: JsonPropertyName("summary")] RunSummary Summary,
    [property: JsonPropertyName("results")] List<ImageRunResult> Results
);

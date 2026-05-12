using ML_Wildboar.Shared.Storage.Entities;

namespace ML_Wildboar.ImageIngest.Services;

public record ImageAnalysisResult(
    bool IsEmpty,
    string Weather,
    string Description,
    IReadOnlyList<AnimalDetection> Detections,
    string RawJson
);

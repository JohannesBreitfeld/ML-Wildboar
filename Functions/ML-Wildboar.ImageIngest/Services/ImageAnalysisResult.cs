using ML_Wildboar.Shared.Storage.Entities;

namespace ML_Wildboar.ImageIngest.Services;

public record ImageAnalysisResult(
    bool IsEmpty,
    string TimeOfDay,
    string Weather,
    string ImageQuality,
    bool ContainsHuman,
    bool ContainsDomestic,
    bool ContainsVehicle,
    string Description,
    IReadOnlyList<AnimalDetection> Detections,
    string RawJson
);

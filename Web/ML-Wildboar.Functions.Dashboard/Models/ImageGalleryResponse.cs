namespace ML_Wildboar.Functions.Dashboard.Models;

public record ImageGalleryResponse(
    List<ImageDto> Images,
    string? ContinuationToken,
    int TotalCount
);

public record ImageDto(
    string Id,
    string PartitionKey,
    string CapturedAt,
    bool IsEmpty,
    string? Weather,
    string? Description,
    List<AnimalDetection> Detections,
    string BlobUrl
);

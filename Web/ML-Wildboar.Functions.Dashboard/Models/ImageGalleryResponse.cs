namespace ML_Wildboar.Functions.Dashboard.Models;

public record ImageGalleryResponse(
    List<ImageDto> Images,
    string? ContinuationToken,
    int TotalCount
);

public record ImageDto(
    string Id,
    DateTime CapturedAt,
    bool ContainsWildboar,
    double ConfidenceScore,
    string ImageUrl
);

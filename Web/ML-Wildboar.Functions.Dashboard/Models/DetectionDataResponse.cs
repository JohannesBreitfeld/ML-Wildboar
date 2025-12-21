namespace ML_Wildboar.Functions.Dashboard.Models;

public record DetectionDataResponse(
    List<DetectionDataPoint> Detections,
    int TotalImages,
    int WildboarImages,
    DateRange DateRange
);

public record DetectionDataPoint(
    DateTime Timestamp,
    int Count,
    double AverageConfidence
);

public record DateRange(
    string Start,
    string End
);

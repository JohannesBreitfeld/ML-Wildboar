using Azure;
using Azure.Data.Tables;

namespace ML_Wildboar.Functions.Dashboard.Entities;

public class ImageRecord : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string BlobStorageUrl { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }

    // Claude analysis results
    public bool IsAnalyzed { get; set; }
    public bool? IsEmpty { get; set; }
    public string? TimeOfDay { get; set; }
    public string? Weather { get; set; }
    public string? ImageQuality { get; set; }
    public bool? ContainsHuman { get; set; }
    public bool? ContainsDomestic { get; set; }
    public bool? ContainsVehicle { get; set; }
    public string? Description { get; set; }
    public string? DetectionsJson { get; set; }
    public string? AnalysisResultBlobUrl { get; set; }
    public DateTime? AnalysisFailedAt { get; set; }

    // Legacy ONNX fields (kept for backward compatibility)
    public bool IsProcessed { get; set; }
    public bool? ContainsWildboar { get; set; }
    public double? ConfidenceScore { get; set; }
}

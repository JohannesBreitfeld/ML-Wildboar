using Azure;
using Azure.Data.Tables;

namespace ML_Wildboar.Shared.Storage.Entities;

/// <summary>
/// Represents an image record stored in Azure Table Storage.
/// PartitionKey is the capture date (yyyy-MM-dd), RowKey is a unique GUID per image.
/// </summary>
public class ImageRecord : ITableEntity
{
    // --- Table Storage required fields ---

    /// <summary>Capture date formatted as yyyy-MM-dd, used for date-based partition queries.</summary>
    public string PartitionKey { get; set; } = string.Empty;

    /// <summary>Unique identifier for this image record within its partition.</summary>
    public string RowKey { get; set; } = string.Empty;

    /// <summary>Managed by Azure Table Storage.</summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>Used for optimistic concurrency by Azure Table Storage.</summary>
    public ETag ETag { get; set; }

    // --- Core image fields ---

    /// <summary>Absolute URL to the image blob in Azure Blob Storage.</summary>
    public string BlobStorageUrl { get; set; } = string.Empty;

    /// <summary>When the camera captured the image (from email metadata).</summary>
    public DateTime CapturedAt { get; set; }

    // --- Claude analysis results ---

    /// <summary>True once Claude has successfully analysed the image.</summary>
    public bool IsAnalyzed { get; set; }

    /// <summary>True if Claude determined no animals are visible in the image.</summary>
    public bool? IsEmpty { get; set; }

    /// <summary>Weather description as returned by Claude, e.g. "mulet" or "klart".</summary>
    public string? Weather { get; set; }

    /// <summary>Swedish free-text description of the scene produced by Claude.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Serialized JSON array of detected animals.
    /// Schema: [{"species":"vildsvin","count":3,"confidence":"hög"}]
    /// Deserialize with System.Text.Json to AnimalDetection[].
    /// </summary>
    public string? DetectionsJson { get; set; }

    /// <summary>URL to the companion blob containing the raw Claude response, used for debugging and reprocessing.</summary>
    public string? AnalysisResultBlobUrl { get; set; }

    /// <summary>Set when analysis fails; used by the retry timer to find unanalysed images.</summary>
    public DateTime? AnalysisFailedAt { get; set; }

    // --- Legacy ONNX fields (no longer populated, kept for backward compatibility) ---

    /// <summary>Legacy: was set by the ONNX processor. No longer used.</summary>
    public bool IsProcessed { get; set; }

    /// <summary>Legacy: ONNX wildboar prediction. No longer populated.</summary>
    public bool? ContainsWildboar { get; set; }

    /// <summary>Legacy: ONNX confidence score (0.0–1.0). No longer populated.</summary>
    public double? ConfidenceScore { get; set; }
}

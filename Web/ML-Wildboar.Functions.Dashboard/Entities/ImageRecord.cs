using Azure;
using Azure.Data.Tables;

namespace ML_Wildboar.Functions.Dashboard.Entities;


/// <summary>
/// Represents an image record stored in Azure Table Storage with wildboar detection prediction results.
/// </summary>
public class ImageRecord : ITableEntity
{
    /// <summary>
    /// Gets or sets the partition key. Uses the date the image was captured (format: yyyy-MM-dd) for partitioning.
    /// </summary>
    public string PartitionKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the row key. Unique identifier for the image record within the partition.
    /// </summary>
    public string RowKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp. Automatically managed by Azure Table Storage.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the ETag for optimistic concurrency.
    /// </summary>
    public ETag ETag { get; set; }

    /// <summary>
    /// Gets or sets the Azure Blob Storage URL where the image is stored.
    /// </summary>
    public string BlobStorageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the image has been processed.
    /// </summary>
    public bool IsProcessed { get; set; }

    /// <summary>
    /// Gets or sets whether the ONNX ML model predicted that the image contains a wildboar.
    /// Null indicates the prediction has not yet been performed.
    /// </summary>
    public bool? ContainsWildboar { get; set; }

    /// <summary>
    /// Gets or sets the confidence score of the wildboar detection prediction (0.0 to 1.0).
    /// Null if no prediction has been made yet.
    /// </summary>
    public double? ConfidenceScore { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the image was captured.
    /// </summary>
    public DateTime CapturedAt { get; set; }

}

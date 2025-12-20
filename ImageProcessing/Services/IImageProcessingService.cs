namespace ML_Wildboar.ImageProcessor.Services;

/// <summary>
/// Service for processing images through ML model and updating records.
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Processes all unprocessed images from storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Processing statistics.</returns>
    Task<ProcessingStatistics> ProcessUnprocessedImagesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Statistics from image processing operation.
/// </summary>
public class ProcessingStatistics
{
    /// <summary>
    /// Total number of images found for processing.
    /// </summary>
    public int TotalImages { get; set; }

    /// <summary>
    /// Number of images successfully processed.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of images that failed processing after all retries.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Number of images where wildboar was detected.
    /// </summary>
    public int WildboarDetected { get; set; }

    /// <summary>
    /// Total duration of processing operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Breakdown of errors by exception type.
    /// </summary>
    public Dictionary<string, int> ErrorCounts { get; set; } = new();
}

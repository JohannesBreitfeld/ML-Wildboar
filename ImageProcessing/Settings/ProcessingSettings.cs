namespace ML_Wildboar.ImageProcessor.Settings;

/// <summary>
/// Configuration settings for image processing operations.
/// </summary>
public class ProcessingSettings
{
    /// <summary>
    /// Maximum number of retry attempts for failed image processing.
    /// Default: 3
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base backoff duration in seconds for exponential backoff.
    /// Actual delays: 1s, 2s, 4s for retries 1, 2, 3.
    /// Default: 1
    /// </summary>
    public int BackoffBaseSeconds { get; set; } = 1;

    /// <summary>
    /// Batch size for processing images.
    /// Default: 100 (process all at once)
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Confidence threshold for wildboar detection (0.0 to 1.0).
    /// Default: 0.75
    /// </summary>
    public double ConfidenceThreshold { get; set; } = 0.75;
}

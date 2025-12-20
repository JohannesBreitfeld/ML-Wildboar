using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ML;
using Microsoft.Extensions.Options;
using ML_Wildboar.ImageProcessor.Settings;
using ML_Wildboar.Shared.Storage.Entities;
using ML_Wildboar.Shared.Storage.Repositories;
using WildboarMonitor_FunctionApp;

namespace ML_Wildboar.ImageProcessor.Services;

/// <summary>
/// Service for processing images through ML.NET model with retry logic.
/// </summary>
public class ImageProcessingService : IImageProcessingService
{
    private readonly IImageRepository _imageRepository;
    private readonly PredictionEnginePool<MLModel.ModelInput, MLModel.ModelOutput> _predictionEnginePool;
    private readonly ProcessingSettings _settings;
    private readonly ILogger<ImageProcessingService> _logger;

    public ImageProcessingService(
        IImageRepository imageRepository,
        PredictionEnginePool<MLModel.ModelInput, MLModel.ModelOutput> predictionEnginePool,
        IOptions<ProcessingSettings> settings,
        ILogger<ImageProcessingService> logger)
    {
        _imageRepository = imageRepository;
        _predictionEnginePool = predictionEnginePool;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProcessingStatistics> ProcessUnprocessedImagesAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var stats = new ProcessingStatistics();

        try
        {
            // Get all unprocessed images
            _logger.LogInformation("Querying for unprocessed images...");
            var unprocessedImages = await _imageRepository.GetUnprocessedImagesAsync();
            stats.TotalImages = unprocessedImages.Count;

            _logger.LogInformation("Found {Count} unprocessed images to process.", stats.TotalImages);

            if (stats.TotalImages == 0)
            {
                _logger.LogInformation("No unprocessed images found. Exiting.");
                return stats;
            }

            // Process each image with retry logic
            foreach (var imageRecord in unprocessedImages)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Processing cancelled by user.");
                    break;
                }

                var success = await ProcessImageWithRetryAsync(imageRecord, stats, cancellationToken);

                if (success)
                {
                    stats.SuccessCount++;
                }
                else
                {
                    stats.FailureCount++;
                }
            }

            stats.Duration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Processing complete. Success: {Success}, Failures: {Failures}, Wildboar detected: {Wildboar}, Duration: {Duration:hh\\:mm\\:ss}",
                stats.SuccessCount, stats.FailureCount, stats.WildboarDetected, stats.Duration);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during image processing");
            stats.Duration = DateTime.UtcNow - startTime;
            throw;
        }
    }

    private async Task<bool> ProcessImageWithRetryAsync(
        ImageRecord imageRecord,
        ProcessingStatistics stats,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt <= _settings.MaxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    // Exponential backoff: 1s, 2s, 4s
                    var delaySeconds = _settings.BackoffBaseSeconds * Math.Pow(2, attempt - 1);
                    _logger.LogInformation(
                        "Retrying image {RowKey} (attempt {Attempt}/{Max}). Waiting {Delay}s...",
                        imageRecord.RowKey, attempt, _settings.MaxRetries, delaySeconds);

                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }

                await ProcessSingleImageAsync(imageRecord, stats, cancellationToken);

                if (attempt > 0)
                {
                    _logger.LogInformation(
                        "Successfully processed image {RowKey} on attempt {Attempt}",
                        imageRecord.RowKey, attempt + 1);
                }

                return true;
            }
            catch (Exception ex) when (attempt < _settings.MaxRetries)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to process image {RowKey} (attempt {Attempt}/{Max}): {Message}",
                    imageRecord.RowKey, attempt + 1, _settings.MaxRetries + 1, ex.Message);

                // Track error type
                var errorType = ex.GetType().Name;
                if (stats.ErrorCounts.TryGetValue(errorType, out var count))
                {
                    stats.ErrorCounts[errorType] = count + 1;
                }
                else
                {
                    stats.ErrorCounts[errorType] = 1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process image {RowKey} after {Attempts} attempts: {Message}",
                    imageRecord.RowKey, _settings.MaxRetries + 1, ex.Message);

                // Track final error
                var errorType = ex.GetType().Name;
                if (stats.ErrorCounts.TryGetValue(errorType, out var count))
                {
                    stats.ErrorCounts[errorType] = count + 1;
                }
                else
                {
                    stats.ErrorCounts[errorType] = 1;
                }

                return false;
            }
        }

        return false;
    }

    private async Task ProcessSingleImageAsync(
        ImageRecord imageRecord,
        ProcessingStatistics stats,
        CancellationToken cancellationToken)
    {
        // Download image from blob storage
        _logger.LogDebug("Downloading image {RowKey} from {BlobUrl}",
            imageRecord.RowKey, imageRecord.BlobStorageUrl);

        var imageBytes = await _imageRepository.DownloadImageFromBlobAsync(imageRecord.BlobStorageUrl);

        // Run through ML model
        var input = new MLModel.ModelInput
        {
            ImageSource = imageBytes
        };

        var prediction = _predictionEnginePool.Predict(input);

        // Extract confidence score for predicted label
        var labelScores = MLModel.GetSortedScoresWithLabels(prediction);
        var topPrediction = labelScores.First();

        // Update image record
        imageRecord.IsProcessed = true;
        imageRecord.ContainsWildboar = prediction.PredictedLabel.Equals(
            "wildboar",
            StringComparison.OrdinalIgnoreCase);
        imageRecord.ConfidenceScore = topPrediction.Value;

        // Save updated record
        await _imageRepository.UpdateImageRecordAsync(imageRecord);

        if (imageRecord.ContainsWildboar == true)
        {
            stats.WildboarDetected++;
        }

        _logger.LogInformation(
            "Processed image {RowKey}: {Prediction} (confidence: {Confidence:P2})",
            imageRecord.RowKey, prediction.PredictedLabel, topPrediction.Value);
    }
}

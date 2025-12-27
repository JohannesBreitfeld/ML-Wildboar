using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ML_Wildboar.ImageProcessor.Services;

namespace ML_Wildboar.ImageProcessor;

/// <summary>
/// Background worker that orchestrates image processing workflow.
/// Runs once and exits when complete.
/// </summary>
public class Worker : BackgroundService
{
    private readonly IImageProcessingService _processingService;
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly ILogger<Worker> _logger;
    private readonly TelemetryClient? _telemetryClient;

    public Worker(
        IImageProcessingService processingService,
        IHostApplicationLifetime hostLifetime,
        ILogger<Worker> logger,
        TelemetryClient? telemetryClient = null)
    {
        _processingService = processingService;
        _hostLifetime = hostLifetime;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("ML-Wildboar Image Processor started at {Time}", DateTimeOffset.UtcNow);

            // Track operation start time for Application Insights
            var operationStartTime = DateTimeOffset.UtcNow;
            var operationId = Guid.NewGuid().ToString();
            bool operationSuccess = false;
            string operationResultCode = "Unknown";

            try
            {
                // Process all unprocessed images
                var stats = await _processingService.ProcessUnprocessedImagesAsync(stoppingToken);

                // Log summary statistics
                _logger.LogInformation(
                    "Processing Summary:\n" +
                    "  Total Images: {Total}\n" +
                    "  Successful: {Success}\n" +
                    "  Failed: {Failed}\n" +
                    "  Wildboar Detected: {Wildboar}\n" +
                    "  Duration: {Duration:hh\\:mm\\:ss}",
                    stats.TotalImages, stats.SuccessCount, stats.FailureCount,
                    stats.WildboarDetected, stats.Duration);

                // Track custom metrics in Application Insights
                if (_telemetryClient != null)
                {
                    _telemetryClient.TrackMetric("ImagesProcessed", stats.SuccessCount);
                    _telemetryClient.TrackMetric("ProcessingFailures", stats.FailureCount);
                    _telemetryClient.TrackMetric("WildboarDetected", stats.WildboarDetected);
                    _telemetryClient.TrackMetric("ProcessingDurationSeconds", stats.Duration.TotalSeconds);

                    // Track error breakdown
                    foreach (var (errorType, count) in stats.ErrorCounts)
                    {
                        _telemetryClient.TrackMetric($"Error_{errorType}", count);
                    }
                }

                operationSuccess = stats.FailureCount == 0;
                operationResultCode = stats.FailureCount > 0 ? "PartialSuccess" : "Success";

                // Set exit code based on results
                Environment.ExitCode = stats.FailureCount > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error during image processing");
                operationSuccess = false;
                operationResultCode = "Error";
                Environment.ExitCode = 1;
                throw;
            }
            finally
            {
                // Track the overall operation in Application Insights
                if (_telemetryClient != null)
                {
                    var requestTelemetry = new RequestTelemetry
                    {
                        Name = "ProcessImages",
                        Id = operationId,
                        Timestamp = operationStartTime,
                        Duration = DateTimeOffset.UtcNow - operationStartTime,
                        Success = operationSuccess,
                        ResponseCode = operationResultCode
                    };
                    _telemetryClient.TrackRequest(requestTelemetry);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Worker failed with exception");
            Environment.ExitCode = 1;
        }
        finally
        {
            if (_telemetryClient != null)
            {
                await _telemetryClient.FlushAsync(stoppingToken);
                // Wait a bit for flush to complete
                await Task.Delay(TimeSpan.FromSeconds(2), CancellationToken.None);
            }

            _logger.LogInformation("ML-Wildboar Image Processor shutting down at {Time}", DateTimeOffset.UtcNow);

            // Signal application to stop
            _hostLifetime.StopApplication();
        }
    }
}

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ML_Wildboar.ImageIngest.Services;
using ML_Wildboar.Shared.Storage.Entities;
using ML_Wildboar.Shared.Storage.Repositories;

namespace ML_Wildboar.ImageIngest.Functions;

public class ImageIngestFunction
{
    private readonly ILogger _logger;
    private readonly IImageExtractor _imageExtractor;
    private readonly IImageRepository _imageRepository;

    public ImageIngestFunction(
        ILoggerFactory loggerFactory,
        IImageExtractor imageExtractor,
        IImageRepository imageRepository)
    {
        _logger = loggerFactory.CreateLogger<ImageIngestFunction>();
        _imageExtractor = imageExtractor;
        _imageRepository = imageRepository;
    }

    [Function("ImageIngestFunction")]
    public async Task Run([TimerTrigger("* * 6 * * *")] TimerInfo myTimer)
    {
        try
        {
            var swedishTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            var localStartTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, swedishTimeZone);
            _logger.LogInformation($"Image ingestion started at {localStartTime:yyyy-MM-dd HH:mm:ss}");

            var latestTimeStamp = await _imageRepository.GetLastCapturedAtAsync();
            var sinceDate = latestTimeStamp ?? DateTime.UtcNow.AddDays(-30);

            _logger.LogInformation("Retrieving images captured after: {sinceDate}", sinceDate);

            var imageAttachments = await _imageExtractor.ExtractAttachments(sinceDate);

            if (imageAttachments is null || imageAttachments.Count == 0)
            {
                _logger.LogWarning($"No image attachments were found after timestamp: {sinceDate}");
                return;
            }

            _logger.LogInformation($"Found {imageAttachments.Count} new image attachments");

            foreach (var attachment in imageAttachments)
            {
                try
                {
                    var blobUrl = await _imageRepository.UploadImageToBlobAsync(
                        attachment.Data,
                        attachment.Id);

                    _logger.LogInformation("Uploaded image to blob: {blobUrl}", blobUrl);

                    var imageRecord = new ImageRecord
                    {
                        PartitionKey = attachment.Timestamp.ToString("yyyy-MM-dd"),
                        RowKey = Guid.NewGuid().ToString(),
                        CapturedAt = attachment.Timestamp,
                        BlobStorageUrl = blobUrl,
                        IsProcessed = false,
                        ContainsWildboar = null,  
                        ConfidenceScore = null   
                    };

                    await _imageRepository.SaveImageRecordAsync(imageRecord);

                    _logger.LogInformation("Saved image record for: {imageId}", attachment.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process image attachment: {imageId}", attachment.Id);
                }
            }

            var localEndTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, swedishTimeZone);
            _logger.LogInformation($"Image ingestion completed at {localEndTime:yyyy-MM-dd HH:mm:ss}. Processed {imageAttachments.Count} images.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image ingestion failed");
            throw;
        }

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
    }
}

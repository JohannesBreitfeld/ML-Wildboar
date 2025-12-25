namespace ML_Wildboar.ImageIngest.Services;

public interface IQueueService
{
    /// <summary>
    /// Sends a message to the image processing queue to trigger the container app job.
    /// </summary>
    /// <param name="imageCount">Number of images that were processed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendProcessingTriggerAsync(int imageCount, CancellationToken cancellationToken = default);
}

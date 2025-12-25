using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ML_Wildboar.Shared.Storage.Settings;
using System.Text.Json;

namespace ML_Wildboar.ImageIngest.Services;

public class QueueService : IQueueService
{
    private readonly QueueClient _queueClient;
    private readonly ILogger<QueueService> _logger;

    public QueueService(
        IOptions<QueueSettings> queueSettings,
        ILogger<QueueService> logger)
    {
        _logger = logger;
        var settings = queueSettings.Value;

        _queueClient = new QueueClient(settings.ConnectionString, settings.QueueName);
    }

    public async Task SendProcessingTriggerAsync(int imageCount, CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure queue exists
            await _queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            // Create message with metadata
            var message = new
            {
                ImageCount = imageCount,
                Timestamp = DateTime.UtcNow,
                TriggerSource = "ImageIngest"
            };

            var messageJson = JsonSerializer.Serialize(message);

            await _queueClient.SendMessageAsync(messageJson, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Sent processing trigger to queue. Image count: {ImageCount}",
                imageCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to processing queue");
            throw;
        }
    }
}

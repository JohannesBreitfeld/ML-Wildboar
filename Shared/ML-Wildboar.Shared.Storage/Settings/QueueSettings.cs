namespace ML_Wildboar.Shared.Storage.Settings;

/// <summary>
/// Configuration settings for Azure Storage Queue.
/// </summary>
public class QueueSettings
{
    /// <summary>
    /// Gets or sets the Azure Storage connection string.
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the name of the Azure Storage Queue.
    /// Defaults to "image-processing-queue".
    /// </summary>
    public string QueueName { get; set; } = "image-processing-queue";
}

namespace ML_Wildboar.Functions.Dashboard.Settings;

/// <summary>
/// Configuration settings for Azure Storage services (Table Storage and Blob Storage).
/// </summary>
public class AzureStorageSettings
{
    /// <summary>
    /// Gets or sets the Azure Storage connection string.
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the name of the Azure Table Storage table for image records.
    /// Defaults to "images".
    /// </summary>
    public string TableName { get; set; } = "images";

    /// <summary>
    /// Gets or sets the name of the Azure Blob Storage container for images.
    /// Defaults to "images".
    /// </summary>
    public string BlobContainerName { get; set; } = "images";
}

using ML_Wildboar.Shared.Storage.Entities;

namespace ML_Wildboar.Shared.Storage.Repositories;

/// <summary>
/// Repository for managing image records and blob storage operations.
/// </summary>
public interface IImageRepository
{
    /// <summary>
    /// Gets the most recent CapturedAt timestamp from all stored image records.
    /// Returns null if no records exist.
    /// </summary>
    Task<DateTime?> GetLastCapturedAtAsync();

    /// <summary>
    /// Uploads an image to Azure Blob Storage.
    /// </summary>
    /// <param name="imageData">The image data as a byte array.</param>
    /// <param name="imageId">Unique identifier for the image (used as blob name).</param>
    /// <returns>The URL of the uploaded blob.</returns>
    Task<string> UploadImageToBlobAsync(byte[] imageData, string imageId);

    /// <summary>
    /// Saves an image record to Azure Table Storage.
    /// </summary>
    /// <param name="record">The image record to save.</param>
    Task SaveImageRecordAsync(ImageRecord record);

    /// <summary>
    /// Gets all unprocessed image records from Azure Table Storage.
    /// </summary>
    /// <returns>A list of image records where IsProcessed is false.</returns>
    Task<List<ImageRecord>> GetUnprocessedImagesAsync();

    /// <summary>
    /// Downloads image data from Azure Blob Storage.
    /// </summary>
    /// <param name="blobUrl">The full URL of the blob to download.</param>
    /// <returns>The image data as a byte array.</returns>
    Task<byte[]> DownloadImageFromBlobAsync(string blobUrl);

    /// <summary>
    /// Updates an existing image record in Azure Table Storage.
    /// </summary>
    /// <param name="record">The image record to update.</param>
    Task UpdateImageRecordAsync(ImageRecord record);

    /// <summary>
    /// Gets image records within a date range, optionally filtered by wildboar detection and confidence.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="containsWildboar">Optional filter for wildboar detection.</param>
    /// <param name="minConfidence">Optional minimum confidence score (0.0-1.0).</param>
    /// <returns>List of matching image records.</returns>
    Task<List<ImageRecord>> GetImagesByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        bool? containsWildboar = null,
        double? minConfidence = null);

    /// <summary>
    /// Gets image records for a specific date with pagination support.
    /// </summary>
    /// <param name="partitionKey">The partition key (date in yyyy-MM-dd format).</param>
    /// <param name="startHour">Optional start hour (0-23).</param>
    /// <param name="endHour">Optional end hour (0-23).</param>
    /// <param name="containsWildboar">Optional filter for wildboar detection.</param>
    /// <param name="pageSize">Number of records per page (default: 50).</param>
    /// <param name="continuationToken">Token for pagination.</param>
    /// <returns>Tuple containing matching records and continuation token.</returns>
    Task<(List<ImageRecord> Records, string? ContinuationToken)> GetImagesByDateAsync(
        string partitionKey,
        int? startHour = null,
        int? endHour = null,
        bool? containsWildboar = null,
        int pageSize = 50,
        string? continuationToken = null);

    /// <summary>
    /// Generates a SAS token URL for accessing a blob with read-only permissions.
    /// </summary>
    /// <param name="blobUrl">The full URL of the blob.</param>
    /// <param name="expiryMinutes">Token expiry time in minutes (default: 60).</param>
    /// <returns>Blob URL with SAS token appended.</returns>
    Task<string> GetBlobSasUrlAsync(string blobUrl, int expiryMinutes = 60);

    /// <summary>
    /// Gets aggregated detection counts by hour for a date range.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="minConfidence">Optional minimum confidence score (0.0-1.0).</param>
    /// <returns>Dictionary mapping DateTime (hour precision) to detection count.</returns>
    Task<Dictionary<DateTime, int>> GetDetectionCountsByHourAsync(
        DateTime startDate,
        DateTime endDate,
        double? minConfidence = null);
}

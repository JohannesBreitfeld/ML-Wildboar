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
}

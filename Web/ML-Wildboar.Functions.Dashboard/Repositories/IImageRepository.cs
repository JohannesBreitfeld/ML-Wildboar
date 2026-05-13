using ML_Wildboar.Functions.Dashboard.Entities;

namespace ML_Wildboar.Functions.Dashboard.Repositories;

public interface IImageRepository
{
    Task<List<ImageRecord>> GetImagesByDateRangeAsync(
        DateTime startDate,
        DateTime endDate);

    Task<(List<ImageRecord> Records, string? ContinuationToken)> GetImagesByDateAsync(
        string partitionKey,
        int pageSize = 100,
        string? continuationToken = null);

    Task<string> GetBlobSasUrlAsync(string blobUrl, int expiryMinutes = 60);

    // Write operations (used by ingest functions, kept for interface completeness)
    Task<string> UploadImageToBlobAsync(byte[] imageData, string imageId);
    Task SaveImageRecordAsync(ImageRecord record);
    Task UpdateImageRecordAsync(ImageRecord record);
    Task<byte[]> DownloadImageFromBlobAsync(string blobUrl);
    Task<List<ImageRecord>> GetUnprocessedImagesAsync();
    Task<DateTime?> GetLastCapturedAtAsync();
}

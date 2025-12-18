using Azure.Data.Tables;
using Azure.Storage.Blobs;
using ML_Wildboar.Shared.Storage.Entities;

namespace ML_Wildboar.Shared.Storage.Repositories;

/// <summary>
/// Implementation of image repository for managing images in Azure Storage.
/// </summary>
public class ImageRepository : IImageRepository
{
    private readonly TableClient _tableClient;
    private readonly BlobContainerClient _blobContainerClient;

    public ImageRepository(
        TableServiceClient tableServiceClient,
        BlobServiceClient blobServiceClient,
        string tableName = "images",
        string containerName = "images")
    {
        _tableClient = tableServiceClient.GetTableClient(tableName);
        _blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetLastCapturedAtAsync()
    {
        try
        {
            await _tableClient.CreateIfNotExistsAsync();

            var query = _tableClient.QueryAsync<ImageRecord>(
                select: new[] { nameof(ImageRecord.CapturedAt) }
            );

            DateTime? lastCaptured = null;

            await foreach (var record in query)
            {
                if (!lastCaptured.HasValue || record.CapturedAt > lastCaptured.Value)
                {
                    lastCaptured = record.CapturedAt;
                }
            }

            return lastCaptured;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string> UploadImageToBlobAsync(byte[] imageData, string imageId)
    {
        await _blobContainerClient.CreateIfNotExistsAsync();

        var blobClient = _blobContainerClient.GetBlobClient($"{imageId}.jpg");

        using var stream = new MemoryStream(imageData);
        await blobClient.UploadAsync(stream, overwrite: true);

        return blobClient.Uri.ToString();
    }

    /// <inheritdoc />
    public async Task SaveImageRecordAsync(ImageRecord record)
    {
        await _tableClient.CreateIfNotExistsAsync();

        await _tableClient.UpsertEntityAsync(record);
    }
}

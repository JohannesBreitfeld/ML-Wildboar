using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using ML_Wildboar.Functions.Dashboard.Entities;

namespace ML_Wildboar.Functions.Dashboard.Repositories;

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

    public async Task<DateTime?> GetLastCapturedAtAsync()
    {
        try
        {
            await _tableClient.CreateIfNotExistsAsync();
            var query = _tableClient.QueryAsync<ImageRecord>(select: [nameof(ImageRecord.CapturedAt)]);
            DateTime? lastCaptured = null;
            await foreach (var record in query)
            {
                if (!lastCaptured.HasValue || record.CapturedAt > lastCaptured.Value)
                    lastCaptured = record.CapturedAt;
            }
            return lastCaptured;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> UploadImageToBlobAsync(byte[] imageData, string imageId)
    {
        await _blobContainerClient.CreateIfNotExistsAsync();
        var blobClient = _blobContainerClient.GetBlobClient(imageId);
        using var stream = new MemoryStream(imageData);
        await blobClient.UploadAsync(stream, overwrite: true);
        return blobClient.Uri.ToString();
    }

    public async Task SaveImageRecordAsync(ImageRecord record)
    {
        await _tableClient.CreateIfNotExistsAsync();
        await _tableClient.UpsertEntityAsync(record);
    }

    public async Task<List<ImageRecord>> GetUnprocessedImagesAsync()
    {
        await _tableClient.CreateIfNotExistsAsync();
        var results = new List<ImageRecord>();
        var query = _tableClient.QueryAsync<ImageRecord>(filter: "IsProcessed eq false");
        await foreach (var record in query)
            results.Add(record);
        return results;
    }

    public async Task<byte[]> DownloadImageFromBlobAsync(string blobUrl)
    {
        var uri = new Uri(blobUrl);
        var blobName = uri.Segments[^1];
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        using var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    public async Task UpdateImageRecordAsync(ImageRecord record)
    {
        await _tableClient.CreateIfNotExistsAsync();
        await _tableClient.UpsertEntityAsync(record, TableUpdateMode.Merge);
    }

    public async Task<List<ImageRecord>> GetImagesByDateRangeAsync(
        DateTime startDate,
        DateTime endDate)
    {
        await _tableClient.CreateIfNotExistsAsync();
        var results = new List<ImageRecord>();

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var partitionKey = date.ToString("yyyy-MM-dd");
            var filter = $"PartitionKey eq '{partitionKey}'";
            var query = _tableClient.QueryAsync<ImageRecord>(filter: filter);

            await foreach (var record in query)
            {
                if (record.CapturedAt >= startDate && record.CapturedAt <= endDate)
                    results.Add(record);
            }
        }

        return results.OrderBy(r => r.CapturedAt).ToList();
    }

    public async Task<(List<ImageRecord> Records, string? ContinuationToken)> GetImagesByDateAsync(
        string partitionKey,
        int pageSize = 100,
        string? continuationToken = null)
    {
        await _tableClient.CreateIfNotExistsAsync();
        var results = new List<ImageRecord>();
        var filter = $"PartitionKey eq '{partitionKey}'";

        var pages = _tableClient.QueryAsync<ImageRecord>(
            filter: filter,
            maxPerPage: pageSize
        ).AsPages(continuationToken);

        await foreach (var page in pages)
        {
            results.AddRange(page.Values);
            return (results, page.ContinuationToken);
        }

        return (results, null);
    }

    public async Task<string> GetBlobSasUrlAsync(string blobUrl, int expiryMinutes = 60)
    {
        var uri = new Uri(blobUrl);
        var blobName = uri.Segments[^1];
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        if (!blobClient.CanGenerateSasUri)
            return blobUrl;

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _blobContainerClient.Name,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }
}

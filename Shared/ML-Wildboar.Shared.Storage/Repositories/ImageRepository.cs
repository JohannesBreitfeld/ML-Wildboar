using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
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

        var blobClient = _blobContainerClient.GetBlobClient(imageId);

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

    /// <inheritdoc />
    public async Task<List<ImageRecord>> GetUnprocessedImagesAsync()
    {
        await _tableClient.CreateIfNotExistsAsync();

        var unprocessedImages = new List<ImageRecord>();

        // Query for records where IsProcessed = false
        var query = _tableClient.QueryAsync<ImageRecord>(
            filter: $"IsProcessed eq false"
        );

        await foreach (var record in query)
        {
            unprocessedImages.Add(record);
        }

        return unprocessedImages;
    }

    /// <inheritdoc />
    public async Task<byte[]> DownloadImageFromBlobAsync(string blobUrl)
    {
        // Extract blob name from URL
        var uri = new Uri(blobUrl);
        var blobName = uri.Segments[^1]; // Last segment is the blob name

        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        using var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);

        return memoryStream.ToArray();
    }

    /// <inheritdoc />
    public async Task UpdateImageRecordAsync(ImageRecord record)
    {
        await _tableClient.CreateIfNotExistsAsync();

        // Use UpsertEntity with Merge mode to update existing entity
        await _tableClient.UpsertEntityAsync(record, TableUpdateMode.Merge);
    }

    /// <inheritdoc />
    public async Task<List<ImageRecord>> GetImagesByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        bool? containsWildboar = null,
        double? minConfidence = null)
    {
        await _tableClient.CreateIfNotExistsAsync();

        var results = new List<ImageRecord>();

        // Query each date partition in the range
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var partitionKey = date.ToString("yyyy-MM-dd");

            var filter = $"PartitionKey eq '{partitionKey}'";

            if (containsWildboar.HasValue)
            {
                filter += $" and ContainsWildboar eq {containsWildboar.Value.ToString().ToLower()}";
            }

            if (minConfidence.HasValue)
            {
                filter += $" and ConfidenceScore ge {minConfidence.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            var query = _tableClient.QueryAsync<ImageRecord>(filter: filter);

            await foreach (var record in query)
            {
                if (record.CapturedAt >= startDate && record.CapturedAt <= endDate)
                {
                    results.Add(record);
                }
            }
        }

        return results.OrderBy(r => r.CapturedAt).ToList();
    }

    /// <inheritdoc />
    public async Task<(List<ImageRecord> Records, string? ContinuationToken)> GetImagesByDateAsync(
        string partitionKey,
        int? startHour = null,
        int? endHour = null,
        bool? containsWildboar = null,
        int pageSize = 50,
        string? continuationToken = null)
    {
        await _tableClient.CreateIfNotExistsAsync();

        var results = new List<ImageRecord>();

        // Build filter for partition
        var filter = $"PartitionKey eq '{partitionKey}'";

        if (containsWildboar.HasValue)
        {
            filter += $" and ContainsWildboar eq {containsWildboar.Value.ToString().ToLower()}";
        }

        // Query with pagination
        var pages = _tableClient.QueryAsync<ImageRecord>(
            filter: filter,
            maxPerPage: pageSize
        ).AsPages(continuationToken);

        await foreach (var page in pages)
        {
            foreach (var record in page.Values)
            {
                // Apply hour filtering if specified
                if (startHour.HasValue && record.CapturedAt.Hour < startHour.Value)
                    continue;

                if (endHour.HasValue && record.CapturedAt.Hour > endHour.Value)
                    continue;

                results.Add(record);
            }

            // Return first page with continuation token
            var nextToken = page.ContinuationToken;
            return (results, nextToken);
        }

        return (results, null);
    }

    /// <inheritdoc />
    public async Task<string> GetBlobSasUrlAsync(string blobUrl, int expiryMinutes = 60)
    {
        // Extract blob name from URL
        var uri = new Uri(blobUrl);
        var blobName = uri.Segments[^1]; // Last segment is the blob name

        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        // Check if the blob client can generate SAS tokens 
        if (!blobClient.CanGenerateSasUri)
        {
            return blobUrl;
        }

        // Generate SAS token with read permissions
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _blobContainerClient.Name,
            BlobName = blobName,
            Resource = "b", // b = blob
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Allow 5 min clock skew
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        return sasUri.ToString();
    }

    /// <inheritdoc />
    public async Task<Dictionary<DateTime, int>> GetDetectionCountsByHourAsync(
        DateTime startDate,
        DateTime endDate,
        double? minConfidence = null)
    {
        var records = await GetImagesByDateRangeAsync(
            startDate,
            endDate,
            containsWildboar: true, 
            minConfidence: minConfidence);

        // Group by hour and count
        var countsByHour = records
            .GroupBy(r => new DateTime(r.CapturedAt.Year, r.CapturedAt.Month, r.CapturedAt.Day, r.CapturedAt.Hour, 0, 0))
            .ToDictionary(g => g.Key, g => g.Count());

        return countsByHour;
    }
}

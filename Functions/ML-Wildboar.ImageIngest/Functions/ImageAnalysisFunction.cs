using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ML_Wildboar.ImageIngest.Services;
using ML_Wildboar.Shared.Storage.Entities;
using ML_Wildboar.Shared.Storage.Repositories;
using System.Text;
using System.Text.Json;

namespace ML_Wildboar.ImageIngest.Functions;

public class ImageAnalysisFunction
{
    private readonly ILogger _logger;
    private readonly IImageAnalysisService _analysisService;
    private readonly IImageRepository _imageRepository;
    private readonly BlobServiceClient _blobServiceClient;

    public ImageAnalysisFunction(
        ILoggerFactory loggerFactory,
        IImageAnalysisService analysisService,
        IImageRepository imageRepository,
        BlobServiceClient blobServiceClient)
    {
        _logger = loggerFactory.CreateLogger<ImageAnalysisFunction>();
        _analysisService = analysisService;
        _imageRepository = imageRepository;
        _blobServiceClient = blobServiceClient;
    }

    [Function("ImageAnalysisFunction")]
    public async Task Run(
        [BlobTrigger("images/{name}", Connection = "BlobStorage")] BlobClient blobClient,
        string name)
    {
        _logger.LogInformation("Starting analysis for blob: {name}", name);

        // Read metadata written by ImageIngestFunction to resolve the Table Storage record.
        var properties = await blobClient.GetPropertiesAsync();
        var metadata = properties.Value.Metadata;

        if (!metadata.TryGetValue("RowKey", out var rowKey) ||
            !metadata.TryGetValue("PartitionKey", out var partitionKey) ||
            !metadata.TryGetValue("CapturedAt", out var capturedAtRaw))
        {
            _logger.LogWarning("Blob {name} has incomplete metadata — skipping analysis", name);
            return;
        }

        var capturedAt = DateTime.SpecifyKind(DateTime.Parse(capturedAtRaw), DateTimeKind.Utc);

        var existing = await _imageRepository.GetImageRecordAsync(partitionKey, rowKey);
        if (existing?.IsAnalyzed == true)
        {
            _logger.LogInformation("Blob {name} already analyzed — skipping", name);
            return;
        }

        // Download image bytes.
        using var ms = new MemoryStream();
        await blobClient.DownloadToAsync(ms);
        var imageData = ms.ToArray();

        ImageAnalysisResult result;
        try
        {
            result = await _analysisService.AnalyzeAsync(imageData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude analysis failed for blob: {name}", name);
            await MarkAnalysisFailedAsync(partitionKey, rowKey, capturedAt);
            return;
        }

        var companionBlobUrl = await SaveCompanionBlobAsync(name, result.RawJson);

        var detections = result.Detections.ToList();
        var containsWildboar = detections.Any(d =>
            d.Species.Equals("vildsvin", StringComparison.OrdinalIgnoreCase));

        var imageRecord = new ImageRecord
        {
            PartitionKey = partitionKey,
            RowKey = rowKey,
            CapturedAt = capturedAt,
            BlobStorageUrl = blobClient.Uri.ToString(),
            IsAnalyzed = true,
            IsEmpty = result.IsEmpty,
            TimeOfDay = result.TimeOfDay,
            Weather = result.Weather,
            ImageQuality = result.ImageQuality,
            ContainsHuman = result.ContainsHuman,
            ContainsDomestic = result.ContainsDomestic,
            ContainsVehicle = result.ContainsVehicle,
            Description = result.Description,
            DetectionsJson = JsonSerializer.Serialize(detections),
            AnalysisResultBlobUrl = companionBlobUrl,
            // Legacy fields — kept populated for backward compatibility with existing dashboard queries.
            IsProcessed = true,
            ContainsWildboar = containsWildboar
        };

        await _imageRepository.UpdateImageRecordAsync(imageRecord);

        _logger.LogInformation(
            "Analysis complete for {name}: isEmpty={isEmpty}, detections={count}",
            name, result.IsEmpty, detections.Count);
    }

    private async Task MarkAnalysisFailedAsync(string partitionKey, string rowKey, DateTime capturedAt)
    {
        var record = new ImageRecord
        {
            PartitionKey = partitionKey,
            RowKey = rowKey,
            CapturedAt = capturedAt,
            AnalysisFailedAt = DateTime.UtcNow
        };
        await _imageRepository.UpdateImageRecordAsync(record);
    }

    private async Task<string> SaveCompanionBlobAsync(string imageBlobName, string json)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("detections");
        await containerClient.CreateIfNotExistsAsync();

        var blobName = Path.GetFileNameWithoutExtension(imageBlobName) + ".json";
        var companion = containerClient.GetBlobClient(blobName);

        var bytes = Encoding.UTF8.GetBytes(json);
        using var stream = new MemoryStream(bytes);
        await companion.UploadAsync(stream, overwrite: true);

        return companion.Uri.ToString();
    }
}

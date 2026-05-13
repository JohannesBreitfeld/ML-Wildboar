using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ML_Wildboar.Functions.Dashboard.Models;
using ML_Wildboar.Functions.Dashboard.Repositories;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ML_Wildboar.Functions.Dashboard.Functions;

public class GetImages(IImageRepository imageRepository, ILogger<GetImages> logger)
{
    [Function("GetImages")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "images")] HttpRequestData req)
    {
        logger.LogInformation("Processing GetImages request");

        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

            var date = query["date"];             // single day: yyyy-MM-dd
            var fromStr = query["from"];           // date range start
            var toStr = query["to"];               // date range end
            var speciesStr = query["species"];     // comma-separated
            var withAnimalsStr = query["withAnimals"];
            var pageSizeStr = query["pageSize"];
            var continuationToken = query["continuationToken"];

            var pageSize = string.IsNullOrEmpty(pageSizeStr) ? 50 : int.Parse(pageSizeStr, CultureInfo.InvariantCulture);
            pageSize = Math.Min(pageSize, 200);

            var speciesFilter = string.IsNullOrEmpty(speciesStr)
                ? []
                : speciesStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            bool? withAnimalsFilter = string.IsNullOrEmpty(withAnimalsStr) ? null : bool.Parse(withAnimalsStr);

            List<Entities.ImageRecord> records;
            string? nextToken = null;

            if (!string.IsNullOrEmpty(date))
            {
                // Single-day query with pagination
                (records, nextToken) = await imageRepository.GetImagesByDateAsync(date, pageSize, continuationToken);
            }
            else if (!string.IsNullOrEmpty(fromStr) || !string.IsNullOrEmpty(toStr))
            {
                var endDate = string.IsNullOrEmpty(toStr)
                    ? DateTime.UtcNow.Date
                    : DateTime.Parse(toStr, CultureInfo.InvariantCulture).Date;
                var startDate = string.IsNullOrEmpty(fromStr)
                    ? endDate.AddDays(-13)
                    : DateTime.Parse(fromStr, CultureInfo.InvariantCulture).Date;

                records = await imageRepository.GetImagesByDateRangeAsync(
                    startDate, endDate.AddDays(1).AddTicks(-1));
            }
            else
            {
                var badRequest = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "Provide 'date' or 'from'/'to' parameters" });
                return badRequest;
            }

            // Filter: only analysed images
            var filtered = records.Where(r => r.IsAnalyzed && r.IsEmpty.HasValue).ToList();

            // Apply withAnimals filter
            if (withAnimalsFilter == true)
                filtered = filtered.Where(r => r.IsEmpty == false).ToList();
            else if (withAnimalsFilter == false)
                filtered = filtered.Where(r => r.IsEmpty == true).ToList();

            // Apply species filter (images that have at least one matching detection)
            if (speciesFilter.Length > 0)
            {
                filtered = filtered.Where(r =>
                {
                    if (r.IsEmpty == true) return false;
                    var detections = ParseDetections(r.DetectionsJson);
                    return detections.Any(d => speciesFilter.Contains(d.Species));
                }).ToList();
            }

            // Sort newest first and limit
            var sorted = filtered.OrderByDescending(r => r.CapturedAt).Take(pageSize).ToList();

            // Map to DTOs with SAS URLs
            var imageDtos = new List<ImageDto>();
            foreach (var record in sorted)
            {
                if (string.IsNullOrEmpty(record.BlobStorageUrl)) continue;
                var blobUrl = await imageRepository.GetBlobSasUrlAsync(record.BlobStorageUrl, expiryMinutes: 60);
                var detections = ParseDetections(record.DetectionsJson);

                imageDtos.Add(new ImageDto(
                    Id: record.RowKey,
                    PartitionKey: record.PartitionKey,
                    CapturedAt: record.CapturedAt.ToString("o"),
                    IsEmpty: record.IsEmpty ?? true,
                    Weather: record.Weather,
                    Description: record.Description,
                    Detections: detections,
                    BlobUrl: blobUrl
                ));
            }

            var response = new ImageGalleryResponse(imageDtos, nextToken, filtered.Count);

            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            await httpResponse.WriteAsJsonAsync(response);
            return httpResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing GetImages request");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to retrieve images" });
            return errorResponse;
        }
    }

    private static List<AnimalDetection> ParseDetections(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<AnimalDetection>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

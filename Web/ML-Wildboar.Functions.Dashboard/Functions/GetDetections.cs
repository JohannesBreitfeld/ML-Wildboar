using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ML_Wildboar.Functions.Dashboard.Models;
using ML_Wildboar.Shared.Storage.Repositories;
using System.Net;

namespace ML_Wildboar.Functions.Dashboard.Functions;

public class GetDetections(IImageRepository imageRepository, ILogger<GetDetections> logger)
{
    [Function("GetDetections")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "detections")] HttpRequestData req)
    {
        logger.LogInformation("Processing GetDetections request");

        try
        {
            // Parse query parameters
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

            var startDateStr = query["startDate"];
            var endDateStr = query["endDate"];
            var minConfidenceStr = query["minConfidence"];
            var groupBy = query["groupBy"] ?? "hour";

            // Default to last 7 days if not specified
            var endDate = string.IsNullOrEmpty(endDateStr)
                ? DateTime.UtcNow
                : DateTime.Parse(endDateStr);

            var startDate = string.IsNullOrEmpty(startDateStr)
                ? endDate.AddDays(-7)
                : DateTime.Parse(startDateStr);

            double? minConfidence = string.IsNullOrEmpty(minConfidenceStr)
                ? null
                : double.Parse(minConfidenceStr);

            // Get all images in range for statistics
            var allImages = await imageRepository.GetImagesByDateRangeAsync(
                startDate, endDate, containsWildboar: null, minConfidence: null);

            var wildboarImages = allImages
                .Where(img => img.ContainsWildboar == true)
                .Where(img => !minConfidence.HasValue || (img.ConfidenceScore ?? 0) >= minConfidence.Value)
                .ToList();

            // Get detection counts by hour
            var countsByHour = await imageRepository.GetDetectionCountsByHourAsync(
                startDate, endDate, minConfidence);

            // Build detection data points
            var detections = countsByHour
                .Select(kvp => new DetectionDataPoint(
                    Timestamp: kvp.Key,
                    Count: kvp.Value,
                    AverageConfidence: wildboarImages
                        .Where(img => new DateTime(img.CapturedAt.Year, img.CapturedAt.Month, img.CapturedAt.Day, img.CapturedAt.Hour, 0, 0) == kvp.Key)
                        .Average(img => img.ConfidenceScore ?? 0)
                ))
                .OrderBy(d => d.Timestamp)
                .ToList();

            // Create response
            var response = new DetectionDataResponse(
                Detections: detections,
                TotalImages: allImages.Count,
                WildboarImages: wildboarImages.Count,
                DateRange: new DateRange(
                    Start: startDate.ToString("yyyy-MM-dd"),
                    End: endDate.ToString("yyyy-MM-dd")
                )
            );

            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            await httpResponse.WriteAsJsonAsync(response);

            return httpResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing GetDetections request");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to retrieve detection data" });

            return errorResponse;
        }
    }
}

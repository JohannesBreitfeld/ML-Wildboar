using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ML_Wildboar.Functions.Dashboard.Models;
using ML_Wildboar.Functions.Dashboard.Repositories;
using System.Globalization;
using System.Net;
using System.Text.Json;

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
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

            // Support both from/to and startDate/endDate for backward compatibility
            var fromStr = query["from"] ?? query["startDate"];
            var toStr = query["to"] ?? query["endDate"];
            var speciesStr = query["species"]; // comma-separated list

            var endDate = string.IsNullOrEmpty(toStr)
                ? DateTime.UtcNow.Date
                : DateTime.Parse(toStr, CultureInfo.InvariantCulture).Date;

            var startDate = string.IsNullOrEmpty(fromStr)
                ? endDate.AddDays(-13)
                : DateTime.Parse(fromStr, CultureInfo.InvariantCulture).Date;

            var speciesFilter = string.IsNullOrEmpty(speciesStr)
                ? []
                : speciesStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Fetch all analysed images in range
            var allImages = await imageRepository.GetImagesByDateRangeAsync(
                startDate,
                endDate.AddDays(1).AddTicks(-1));

            var analysed = allImages.Where(img => img.IsAnalyzed && img.IsEmpty.HasValue).ToList();

            // Overall stats (ignore species filter)
            var totalImages = analysed.Count;
            var withAnimals = analysed.Count(img => img.IsEmpty == false);
            var empty = analysed.Count(img => img.IsEmpty == true);

            // Build day-range lookup
            var days = new List<string>();
            for (var d = startDate; d <= endDate; d = d.AddDays(1))
                days.Add(d.ToString("yyyy-MM-dd"));

            // Daily aggregation
            var dailyMap = days.ToDictionary(d => d, d => new DailyAggMutable { Date = d });

            foreach (var img in analysed)
            {
                var dayKey = img.CapturedAt.ToString("yyyy-MM-dd");
                if (!dailyMap.TryGetValue(dayKey, out var row)) continue;

                row.Total++;
                if (img.IsEmpty == true)
                {
                    row.Empty++;
                }
                else
                {
                    row.WithAnimals++;
                    // bySpecies: only include detections that match species filter (or all if no filter)
                    var detections = ParseDetections(img.DetectionsJson);
                    foreach (var det in detections)
                    {
                        if (speciesFilter.Length > 0 && !speciesFilter.Contains(det.Species))
                            continue;
                        row.BySpecies.TryGetValue(det.Species, out var existing);
                        row.BySpecies[det.Species] = existing + det.Count;
                    }
                }
            }

            var dailyAgg = days.Select(d => dailyMap[d].ToRecord()).ToList();

            // Hourly aggregation (images with animals, respecting species filter)
            var hourlyArr = Enumerable.Range(0, 24)
                .Select(h => new HourlyAggMutable { Hour = h })
                .ToArray();

            foreach (var img in analysed.Where(img => img.IsEmpty == false))
            {
                var row = hourlyArr[img.CapturedAt.Hour];
                var detections = ParseDetections(img.DetectionsJson);
                foreach (var det in detections)
                {
                    if (speciesFilter.Length > 0 && !speciesFilter.Contains(det.Species))
                        continue;
                    row.BySpecies.TryGetValue(det.Species, out var existing);
                    row.BySpecies[det.Species] = existing + det.Count;
                    row.Total += det.Count;
                }
            }

            var hourlyAgg = hourlyArr.Select(h => h.ToRecord()).ToList();

            var response = new DashboardDataResponse(dailyAgg, hourlyAgg, totalImages, withAnimals, empty);

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

    private class DailyAggMutable
    {
        public string Date { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Empty { get; set; }
        public int WithAnimals { get; set; }
        public Dictionary<string, int> BySpecies { get; set; } = [];
        public DailyAgg ToRecord() => new(Date, Total, Empty, WithAnimals, BySpecies);
    }

    private class HourlyAggMutable
    {
        public int Hour { get; set; }
        public int Total { get; set; }
        public Dictionary<string, int> BySpecies { get; set; } = [];
        public HourlyAgg ToRecord() => new(Hour, Total, BySpecies);
    }
}

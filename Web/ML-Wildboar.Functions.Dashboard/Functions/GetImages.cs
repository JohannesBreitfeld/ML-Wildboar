using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ML_Wildboar.Functions.Dashboard.Models;
using ML_Wildboar.Shared.Storage.Repositories;
using System.Globalization;
using System.Net;

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
            // Parse query parameters
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

            var date = query["date"];
            var startHourStr = query["startHour"];
            var endHourStr = query["endHour"];
            var containsWildboarStr = query["containsWildboar"];
            var minConfidenceStr = query["minConfidence"];
            var pageSizeStr = query["pageSize"];
            var continuationToken = query["continuationToken"];

            // Validate required parameter
            if (string.IsNullOrEmpty(date))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "Date parameter is required (format: yyyy-MM-dd)" });
                return badRequest;
            }

            // Parse optional parameters
            int? startHour = string.IsNullOrEmpty(startHourStr) ? null : int.Parse(startHourStr, CultureInfo.InvariantCulture);
            int? endHour = string.IsNullOrEmpty(endHourStr) ? null : int.Parse(endHourStr, CultureInfo.InvariantCulture);
            bool? containsWildboar = string.IsNullOrEmpty(containsWildboarStr) ? null : bool.Parse(containsWildboarStr);
            int pageSize = string.IsNullOrEmpty(pageSizeStr) ? 50 : int.Parse(pageSizeStr, CultureInfo.InvariantCulture);

            // Limit page size
            pageSize = Math.Min(pageSize, 100);

            // Get images for the specified date
            var (records, nextToken) = await imageRepository.GetImagesByDateAsync(
                partitionKey: date,
                startHour: startHour,
                endHour: endHour,
                containsWildboar: containsWildboar,
                pageSize: pageSize,
                continuationToken: continuationToken
            );

            // Filter by confidence if specified
            if (!string.IsNullOrEmpty(minConfidenceStr))
            {
                var minConfidence = double.Parse(minConfidenceStr, CultureInfo.InvariantCulture);
                records = records.Where(r => (r.ConfidenceScore ?? 0) >= minConfidence).ToList();
            }

            // Generate SAS tokens for images
            var imageDtos = new List<ImageDto>();
            foreach (var record in records)
            {
                var imageUrl = await imageRepository.GetBlobSasUrlAsync(record.BlobStorageUrl, expiryMinutes: 60);

                imageDtos.Add(new ImageDto(
                    Id: record.RowKey,
                    CapturedAt: record.CapturedAt,
                    ContainsWildboar: record.ContainsWildboar ?? false,
                    ConfidenceScore: record.ConfidenceScore ?? 0,
                    ImageUrl: imageUrl
                ));
            }

            // Create response
            var response = new ImageGalleryResponse(
                Images: imageDtos,
                ContinuationToken: nextToken,
                TotalCount: records.Count
            );

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
}

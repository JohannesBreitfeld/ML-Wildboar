using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ML_Wildboar.Functions.Dashboard.Models;
using ML_Wildboar.Shared.Storage.Repositories;
using System.Globalization;
using System.Net;

namespace ML_Wildboar.Functions.Dashboard.Functions;

public class GetImageSasToken(IImageRepository imageRepository, ILogger<GetImageSasToken> logger)
{
    [Function("GetImageSasToken")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "images/sas")] HttpRequestData req)
    {
        logger.LogInformation("Processing GetImageSasToken request");

        try
        {
            // Parse query parameters
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var blobUrl = query["blobUrl"];
            var expiryMinutesStr = query["expiryMinutes"];

            // Validate required parameter
            if (string.IsNullOrEmpty(blobUrl))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "blobUrl parameter is required" });
                return badRequest;
            }

            var expiryMinutes = string.IsNullOrEmpty(expiryMinutesStr) ? 60 : int.Parse(expiryMinutesStr, CultureInfo.InvariantCulture);

            // Limit expiry to max 24 hours
            expiryMinutes = Math.Min(expiryMinutes, 1440);

            // Generate SAS token for the provided blob URL
            var sasUrl = await imageRepository.GetBlobSasUrlAsync(blobUrl, expiryMinutes);

            // Create response
            var response = new SasTokenResponse(
                ImageUrl: sasUrl,
                ExpiresAt: DateTime.UtcNow.AddMinutes(expiryMinutes)
            );

            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            await httpResponse.WriteAsJsonAsync(response);

            return httpResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing GetImageSasToken request");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to generate SAS token" });

            return errorResponse;
        }
    }
}

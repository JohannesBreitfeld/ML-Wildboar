using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using ML_Wildboar.ImageIngest.Models;
using ML_Wildboar.ImageIngest.Settings;

namespace ML_Wildboar.ImageIngest.Services;

public class ImageExtractor : IImageExtractor
{
    private readonly GmailSettings _settings;

    public ImageExtractor(IOptions<GmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<GmailService> StartService()
    {
        UserCredential credential;

        if (!string.IsNullOrEmpty(_settings.RefreshToken))
        {
            var token = new Google.Apis.Auth.OAuth2.Responses.TokenResponse
            {
                RefreshToken = _settings.RefreshToken
            };

            credential = new UserCredential(
                new GoogleAuthorizationCodeFlow(
                    new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = new ClientSecrets
                        {
                            ClientId = _settings.ClientId,
                            ClientSecret = _settings.ClientSecret
                        },
                        Scopes = new[] { GmailService.Scope.GmailReadonly }
                    }
                ),
                "user",
                token
            );
        }
        else
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = _settings.ClientId,
                    ClientSecret = _settings.ClientSecret
                },
                new[] { GmailService.Scope.GmailReadonly },
                "user",
                CancellationToken.None
            );
        }

        var service = new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential
        });

        return service;
    }

    public async Task<HashSet<ImageAttachment>> ExtractAttachments(DateTime lastRetrievedMessageDate)
    {
        using var service = await StartService();

        HashSet<ImageAttachment> attachments = [];

        var dateFilter = lastRetrievedMessageDate.AddDays(-1).ToString("yyyy/MM/dd");
        var allThreads = new List<Google.Apis.Gmail.v1.Data.Thread>();
        string? nextPageToken = null;

        do
        {
            var request = service.Users.Threads.List("me");

            request.Q = $"after:{dateFilter}";
            request.MaxResults = 500;
            request.PageToken = nextPageToken;

            var response = request.Execute();
            allThreads.AddRange(response.Threads);
            nextPageToken = response.NextPageToken;

        } while (nextPageToken != null);

        foreach (var thread in allThreads)
        {
            var threadRequest = service.Users.Threads.Get("me", thread.Id);
            var threadData = threadRequest.Execute();

            foreach (var message in threadData.Messages)
            {
                foreach (var part in message.Payload.Parts)
                {
                    if (string.IsNullOrEmpty(part.Filename))
                    {
                        continue;
                    }
                    DateTime messageDate = UnixTimeToDateTime(message.InternalDate!.Value);

                    //Since gmail only allow to filter by day i need to refilter by time here.
                    if (messageDate <= lastRetrievedMessageDate)
                    {
                        continue;
                    }

                    var attachId = part.Body.AttachmentId;
                    var attachRequest = service.Users.Messages.Attachments.Get("me", message.Id, attachId);
                    var attachData = attachRequest.Execute();
                    byte[] data = Convert.FromBase64String(attachData.Data.Replace('-', '+').Replace('_', '/'));

                    var fileName = message.Id + part.Filename;

                    var attachment = new ImageAttachment() { Id = fileName, Timestamp = messageDate, Data = data };
                    attachments.Add(attachment);
                }
            }
        }
        return attachments;
    }

    public DateTime UnixTimeToDateTime(long unixTime)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(unixTime).UtcDateTime;
    }
}

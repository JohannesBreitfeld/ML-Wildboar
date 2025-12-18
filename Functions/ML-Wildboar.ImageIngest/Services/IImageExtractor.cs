using Google.Apis.Gmail.v1;
using ML_Wildboar.ImageIngest.Models;

namespace ML_Wildboar.ImageIngest.Services;

public interface IImageExtractor
{
    Task<HashSet<ImageAttachment>> ExtractAttachments(DateTime lastRetrievedMessageDate);
    Task<GmailService> StartService();
}

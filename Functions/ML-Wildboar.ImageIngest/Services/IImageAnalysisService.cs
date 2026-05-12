namespace ML_Wildboar.ImageIngest.Services;

public interface IImageAnalysisService
{
    Task<ImageAnalysisResult> AnalyzeAsync(byte[] imageData);
}

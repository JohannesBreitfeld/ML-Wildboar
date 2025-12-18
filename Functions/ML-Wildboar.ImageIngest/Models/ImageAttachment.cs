namespace ML_Wildboar.ImageIngest.Models;

public class ImageAttachment
{
    public string Id { get; set; } = string.Empty;

    public byte[] Data { get; set; } = [];

    public DateTime Timestamp { get; set; }
}

namespace ML_Wildboar.ImageIngest.Settings;

public class GmailSettings
{
    public required string ClientId { get; set; }

    public required string ClientSecret { get; set; }

    /// <summary>
    /// Optional refresh token for unattended authentication in Azure.
    /// If provided, will be used instead of interactive OAuth flow.
    /// </summary>
    public string? RefreshToken { get; set; }
}

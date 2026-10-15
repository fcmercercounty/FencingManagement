namespace FencingClub.Api.Services;

public class NotificationHubOptions
{
    public const string SectionName = "NotificationHub";

    /// <summary>The hub's DefaultFullSharedAccessSignature connection string. Leave empty to log pushes instead of sending them.</summary>
    public string? ConnectionString { get; set; }
    public string? HubName { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString) && !string.IsNullOrWhiteSpace(HubName);
}

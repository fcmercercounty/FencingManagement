using FencingClub.Api.Dtos;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Options;

namespace FencingClub.Api.Services;

/// <summary>
/// Azure Notification Hubs implementation. Each device gets a platform-specific template with
/// $(title) and $(body) placeholders, so one SendAsync call reaches iOS, Android and Windows alike.
/// </summary>
public class NotificationHubPushService : IPushNotificationService
{
    private const string TemplateName = "generic";

    private readonly NotificationHubClient _hub;
    private readonly ILogger<NotificationHubPushService> _logger;

    public NotificationHubPushService(IOptions<NotificationHubOptions> options, ILogger<NotificationHubPushService> logger)
    {
        var o = options.Value;
        _hub = NotificationHubClient.CreateClientFromConnectionString(o.ConnectionString, o.HubName);
        _logger = logger;
    }

    public async Task RegisterDeviceAsync(string userId, DeviceRegistrationRequest request, CancellationToken ct = default)
    {
        // Tags are set by the server, never by the client, so a device can only subscribe to its own user.
        var tags = new List<string> { IPushNotificationService.UserTag(userId) };
        tags.AddRange((request.ClubIds ?? []).Select(IPushNotificationService.ClubTag));

        var (platform, template) = request.Platform switch
        {
            DevicePlatform.Apns => (NotificationPlatform.Apns,
                """{"aps":{"alert":{"title":"$(title)","body":"$(body)"}}}"""),
            DevicePlatform.Fcm => (NotificationPlatform.FcmV1,
                """{"message":{"notification":{"title":"$(title)","body":"$(body)"}}}"""),
            DevicePlatform.Wns => (NotificationPlatform.Wns,
                """<toast><visual><binding template="ToastGeneric"><text id="1">$(title)</text><text id="2">$(body)</text></binding></visual></toast>"""),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Platform, "Unsupported platform"),
        };

        var installation = new Installation
        {
            InstallationId = request.InstallationId,
            Platform = platform,
            PushChannel = request.PushChannel,
            Tags = tags,
            Templates = new Dictionary<string, InstallationTemplate>
            {
                [TemplateName] = new() { Body = template },
            },
        };
        if (platform == NotificationPlatform.Wns)
            installation.Templates[TemplateName].Headers = new Dictionary<string, string> { ["X-WNS-Type"] = "wns/toast" };

        await _hub.CreateOrUpdateInstallationAsync(installation, ct);
    }

    public Task UnregisterDeviceAsync(string installationId, CancellationToken ct = default) =>
        _hub.DeleteInstallationAsync(installationId, ct);

    public async Task SendAsync(string tagExpression, string title, string body, CancellationToken ct = default)
    {
        var properties = new Dictionary<string, string> { ["title"] = title, ["body"] = body };
        var outcome = await _hub.SendTemplateNotificationAsync(properties, tagExpression, ct);
        _logger.LogInformation("Push to {Tags}: {State} ({TrackingId})", tagExpression, outcome.State, outcome.TrackingId);
    }
}

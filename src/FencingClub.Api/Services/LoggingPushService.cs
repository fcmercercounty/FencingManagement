using FencingClub.Api.Dtos;

namespace FencingClub.Api.Services;

/// <summary>Used when no Notification Hub is configured (local dev, tests). Logs instead of sending.</summary>
public class LoggingPushService(ILogger<LoggingPushService> logger) : IPushNotificationService
{
    public Task RegisterDeviceAsync(string userId, DeviceRegistrationRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[push disabled] register {InstallationId} ({Platform}) for user {UserId}",
            request.InstallationId, request.Platform, userId);
        return Task.CompletedTask;
    }

    public Task UnregisterDeviceAsync(string installationId, CancellationToken ct = default)
    {
        logger.LogInformation("[push disabled] unregister {InstallationId}", installationId);
        return Task.CompletedTask;
    }

    public Task SendAsync(string tagExpression, string title, string body, CancellationToken ct = default)
    {
        logger.LogInformation("[push disabled] to {Tags}: {Title} - {Body}", tagExpression, title, body);
        return Task.CompletedTask;
    }
}

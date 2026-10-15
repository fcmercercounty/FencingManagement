using FencingClub.Api.Dtos;

namespace FencingClub.Api.Services;

/// <summary>
/// Push notifications to phones and desktops. Devices are tagged so you can target
/// one user ("user:{id}") or everyone following a club ("club:{id}").
/// </summary>
public interface IPushNotificationService
{
    Task RegisterDeviceAsync(string userId, DeviceRegistrationRequest request, CancellationToken ct = default);
    Task UnregisterDeviceAsync(string installationId, CancellationToken ct = default);
    Task SendAsync(string tagExpression, string title, string body, CancellationToken ct = default);

    static string UserTag(string userId) => $"user:{userId}";
    static string ClubTag(int clubId) => $"club:{clubId}";
}

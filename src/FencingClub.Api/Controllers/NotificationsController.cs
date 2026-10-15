using FencingClub.Api.Dtos;
using FencingClub.Api.Models;
using FencingClub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FencingClub.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(IPushNotificationService push) : ControllerBase
{
    /// <summary>Register (or refresh) this device for push. Call on every app start; push tokens change.</summary>
    /// <remarks>
    /// Installation ids are chosen by the device, so a production app should track which user owns each id
    /// before letting a caller overwrite or delete it.
    /// </remarks>
    [HttpPut("installations")]
    public async Task<IActionResult> RegisterDevice(DeviceRegistrationRequest request, CancellationToken ct)
    {
        await push.RegisterDeviceAsync(User.GetUserId()!, request, ct);
        return NoContent();
    }

    [HttpDelete("installations/{installationId}")]
    public async Task<IActionResult> UnregisterDevice(string installationId, CancellationToken ct)
    {
        await push.UnregisterDeviceAsync(installationId, ct);
        return NoContent();
    }

    /// <summary>Send a push to one user, everyone following a club, or both.</summary>
    [HttpPost("send")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> Send(SendNotificationRequest request, CancellationToken ct)
    {
        var tags = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.UserId))
            tags.Add(IPushNotificationService.UserTag(request.UserId));
        if (request.ClubId is not null)
            tags.Add(IPushNotificationService.ClubTag(request.ClubId.Value));
        if (tags.Count == 0)
            return ValidationProblem("Provide a UserId, a ClubId, or both.");

        await push.SendAsync(string.Join(" || ", tags), request.Title, request.Body, ct);
        return Accepted();
    }
}

using System.Net;
using System.Net.Http.Json;
using FencingClub.Api.Dtos;

namespace FencingClub.Api.Tests;

/// <summary>No Notification Hub is configured in tests, so these exercise the logging fallback.</summary>
public class NotificationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Device_registration_and_send_succeed_without_a_hub()
    {
        var admin = await factory.CreateClientAsAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);

        var register = await admin.PutAsJsonAsync("/api/notifications/installations",
            new DeviceRegistrationRequest("device-1", DevicePlatform.Fcm, "token", [1]), TestJson.Options);
        Assert.Equal(HttpStatusCode.NoContent, register.StatusCode);

        var send = await admin.PostAsJsonAsync("/api/notifications/send",
            new SendNotificationRequest("Hello", "Practice moved to 7pm", null, 1));
        Assert.Equal(HttpStatusCode.Accepted, send.StatusCode);
    }

    [Fact]
    public async Task Send_without_a_target_is_rejected()
    {
        var admin = await factory.CreateClientAsAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        var send = await admin.PostAsJsonAsync("/api/notifications/send",
            new SendNotificationRequest("Hello", "Nobody", null, null));
        Assert.Equal(HttpStatusCode.BadRequest, send.StatusCode);
    }
}

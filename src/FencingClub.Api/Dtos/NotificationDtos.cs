using System.ComponentModel.DataAnnotations;

namespace FencingClub.Api.Dtos;

public enum DevicePlatform
{
    Apns,
    Fcm,
    Wns,
}

/// <summary>Sent by a mobile/desktop app after it gets a push token from the OS.</summary>
public record DeviceRegistrationRequest(
    [Required, MaxLength(200)] string InstallationId,
    DevicePlatform Platform,
    [Required] string PushChannel,
    int[]? ClubIds);

public record SendNotificationRequest(
    [Required, MaxLength(100)] string Title,
    [Required, MaxLength(1000)] string Body,
    string? UserId,
    int? ClubId);

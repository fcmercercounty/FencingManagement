using System.ComponentModel.DataAnnotations;

namespace FencingClub.Api.Services;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;

    /// <summary>HMAC key, at least 32 characters. Keep it in user secrets locally and in Key Vault / App Service settings in Azure.</summary>
    [Required, MinLength(32)] public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)] public int AccessTokenMinutes { get; set; } = 60;
}

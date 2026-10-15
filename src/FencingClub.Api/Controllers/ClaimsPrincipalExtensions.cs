using System.Security.Claims;

namespace FencingClub.Api.Controllers;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in user's id (the JWT "sub" claim).</summary>
    public static string? GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
}

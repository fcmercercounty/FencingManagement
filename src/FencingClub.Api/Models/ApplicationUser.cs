using Microsoft.AspNetCore.Identity;

namespace FencingClub.Api.Models;

/// <summary>An account that can sign in. Extends ASP.NET Core Identity's user.</summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

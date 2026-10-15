using System.ComponentModel.DataAnnotations;

namespace FencingClub.Api.Dtos;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserResponse User);

public record UserResponse(string Id, string Email, string FirstName, string LastName, IReadOnlyList<string> Roles);

public record AssignRoleRequest([Required] string Role);

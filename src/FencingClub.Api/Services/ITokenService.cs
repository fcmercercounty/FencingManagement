using FencingClub.Api.Models;

namespace FencingClub.Api.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(ApplicationUser user, IEnumerable<string> roles);
}

using FencingClub.Api.Dtos;
using FencingClub.Api.Models;
using FencingClub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FencingClub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokens) : ControllerBase
{
    /// <summary>Create an account. New accounts get the Member role.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return IdentityProblem(result);

        await userManager.AddToRoleAsync(user, Roles.Member);
        return await CreateAuthResponse(user);
    }

    /// <summary>Exchange email and password for a bearer token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized();

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
            return Problem("Account locked after too many failed attempts. Try again later.", statusCode: StatusCodes.Status423Locked);
        if (!result.Succeeded)
            return Unauthorized();

        return await CreateAuthResponse(user);
    }

    /// <summary>The signed-in user's profile and roles.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> Me()
    {
        var user = await userManager.FindByIdAsync(User.GetUserId() ?? string.Empty);
        if (user is null)
            return Unauthorized();
        return await ToUserResponse(user);
    }

    /// <summary>Grant a role (Admin, Coach, Member) to a user.</summary>
    [HttpPost("users/{userId}/roles")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserResponse>> AssignRole(string userId, AssignRoleRequest request)
    {
        if (!Roles.All.Contains(request.Role))
            return ValidationProblem($"Role must be one of: {string.Join(", ", Roles.All)}");

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        if (!await userManager.IsInRoleAsync(user, request.Role))
        {
            var result = await userManager.AddToRoleAsync(user, request.Role);
            if (!result.Succeeded)
                return IdentityProblem(result);
        }
        return await ToUserResponse(user);
    }

    private async Task<AuthResponse> CreateAuthResponse(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (token, expires) = tokens.CreateAccessToken(user, roles);
        return new AuthResponse(token, expires, new UserResponse(user.Id, user.Email!, user.FirstName, user.LastName, roles.ToList()));
    }

    private async Task<UserResponse> ToUserResponse(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserResponse(user.Id, user.Email!, user.FirstName, user.LastName, roles.ToList());
    }

    private ActionResult IdentityProblem(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(error.Code, error.Description);
        return ValidationProblem(ModelState);
    }
}

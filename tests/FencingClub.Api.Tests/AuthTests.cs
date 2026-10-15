using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FencingClub.Api.Dtos;

namespace FencingClub.Api.Tests;

public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Register_then_me_returns_member_profile()
    {
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("ada@example.com", "Passw0rd!", "Ada", "Lovelace"));
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var me = await client.GetFromJsonAsync<UserResponse>("/api/auth/me", TestJson.Options);
        Assert.Equal("ada@example.com", me!.Email);
        Assert.Contains("Member", me.Roles);
    }

    [Fact]
    public async Task Login_with_wrong_password_is_unauthorized()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new LoginRequest(ApiFactory.AdminEmail, "wrong-password"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_without_token_is_unauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/clubs");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_email_is_rejected()
    {
        var client = factory.CreateClient();
        var request = new RegisterRequest("dupe@example.com", "Passw0rd!", "D", "Upe");
        (await client.PostAsJsonAsync("/api/auth/register", request)).EnsureSuccessStatusCode();
        var second = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }
}

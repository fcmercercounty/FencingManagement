using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FencingClub.Api.Dtos;
using FencingClub.Api.Models;
using FencingClub.Api.Repositories;

namespace FencingClub.Api.Tests;

public class CrudTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Admin_can_create_read_update_and_delete_a_club()
    {
        var admin = await factory.CreateClientAsAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);

        var created = await admin.PostAsJsonAsync("/api/clubs", new ClubRequest("Salle Test", "Austin", null));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var club = await created.Content.ReadFromJsonAsync<ClubResponse>(TestJson.Options);

        var fetched = await admin.GetFromJsonAsync<ClubResponse>($"/api/clubs/{club!.Id}", TestJson.Options);
        Assert.Equal("Salle Test", fetched!.Name);

        var updated = await admin.PutAsJsonAsync($"/api/clubs/{club.Id}", new ClubRequest("Salle Renamed", "Austin", null));
        updated.EnsureSuccessStatusCode();
        Assert.Equal("Salle Renamed", (await updated.Content.ReadFromJsonAsync<ClubResponse>(TestJson.Options))!.Name);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/clubs/{club.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/clubs/{club.Id}")).StatusCode);
    }

    [Fact]
    public async Task Member_can_read_clubs_but_not_create_them()
    {
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("member@example.com", "Passw0rd!", "Mem", "Ber"));
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/clubs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync("/api/clubs", new ClubRequest("Nope", null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/members")).StatusCode);
    }

    [Fact]
    public async Task Staff_can_add_members_and_schedule_classes()
    {
        var admin = await factory.CreateClientAsAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        var club = await (await admin.PostAsJsonAsync("/api/clubs", new ClubRequest("Classes Club", null, null)))
            .Content.ReadFromJsonAsync<ClubResponse>(TestJson.Options);

        var member = await admin.PostAsJsonAsync("/api/members",
            new MemberRequest(club!.Id, "Epee", "Fencer", "epee@example.com", Weapon.Epee));
        Assert.Equal(HttpStatusCode.Created, member.StatusCode);

        var members = await admin.GetFromJsonAsync<PagedResult<MemberResponse>>($"/api/members?clubId={club.Id}", TestJson.Options);
        Assert.Single(members!.Items);

        var cls = await admin.PostAsJsonAsync("/api/classes",
            new ClassRequest(club.Id, "Beginner Foil", Weapon.Foil, DateTime.UtcNow.AddDays(3), 90, 12, "Coach Kim"));
        Assert.Equal(HttpStatusCode.Created, cls.StatusCode);

        var badClub = await admin.PostAsJsonAsync("/api/members",
            new MemberRequest(999_999, "No", "Club", null, Weapon.Sabre));
        Assert.Equal(HttpStatusCode.BadRequest, badClub.StatusCode);
    }

    [Fact]
    public async Task Invalid_payload_returns_validation_problem()
    {
        var admin = await factory.CreateClientAsAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        var response = await admin.PostAsJsonAsync("/api/clubs", new { name = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var response = await factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

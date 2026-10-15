using FencingClub.Api.Dtos;
using FencingClub.Api.Models;
using FencingClub.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FencingClub.Api.Controllers;

[ApiController]
[Route("api/clubs")]
[Authorize]
public class ClubsController(IRepository<Club> clubs) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<ClubResponse>> List(string? search, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        var result = await clubs.ListAsync(
            string.IsNullOrWhiteSpace(search) ? null : c => c.Name.Contains(search),
            page, pageSize, ct);
        return new PagedResult<ClubResponse>(result.Items.Select(c => c.ToResponse()).ToList(), result.Page, result.PageSize, result.TotalCount);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClubResponse>> Get(int id, CancellationToken ct)
    {
        var club = await clubs.GetAsync(id, ct);
        return club is null ? NotFound() : club.ToResponse();
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ClubResponse>> Create(ClubRequest request, CancellationToken ct)
    {
        var club = new Club();
        club.Apply(request);
        await clubs.AddAsync(club, ct);
        return CreatedAtAction(nameof(Get), new { id = club.Id }, club.ToResponse());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ClubResponse>> Update(int id, ClubRequest request, CancellationToken ct)
    {
        var club = await clubs.GetAsync(id, ct);
        if (club is null)
            return NotFound();

        club.Apply(request);
        await clubs.UpdateAsync(club, ct);
        return club.ToResponse();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var club = await clubs.GetAsync(id, ct);
        if (club is null)
            return NotFound();

        await clubs.DeleteAsync(club, ct);
        return NoContent();
    }
}

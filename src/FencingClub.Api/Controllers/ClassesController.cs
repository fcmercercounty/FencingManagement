using FencingClub.Api.Dtos;
using FencingClub.Api.Models;
using FencingClub.Api.Repositories;
using FencingClub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FencingClub.Api.Controllers;

[ApiController]
[Route("api/classes")]
[Authorize]
public class ClassesController(
    IRepository<FencingClass> classes,
    IRepository<Club> clubs,
    IPushNotificationService push) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<ClassResponse>> List(int? clubId, DateTime? from, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        var result = await classes.ListAsync(
            c => (clubId == null || c.ClubId == clubId) && (from == null || c.StartsAt >= from),
            page, pageSize, ct);
        return new PagedResult<ClassResponse>(result.Items.Select(c => c.ToResponse()).ToList(), result.Page, result.PageSize, result.TotalCount);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClassResponse>> Get(int id, CancellationToken ct)
    {
        var item = await classes.GetAsync(id, ct);
        return item is null ? NotFound() : item.ToResponse();
    }

    /// <summary>Schedule a class and notify devices following that club.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Staff)]
    public async Task<ActionResult<ClassResponse>> Create(ClassRequest request, CancellationToken ct)
    {
        if (!await clubs.ExistsAsync(request.ClubId, ct))
            return ValidationProblem($"Club {request.ClubId} does not exist.");

        var item = new FencingClass();
        item.Apply(request);
        await classes.AddAsync(item, ct);

        await push.SendAsync(
            IPushNotificationService.ClubTag(item.ClubId),
            "New class scheduled",
            $"{item.Title} on {item.StartsAt:ddd d MMM, HH:mm}",
            ct);

        return CreatedAtAction(nameof(Get), new { id = item.Id }, item.ToResponse());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<ActionResult<ClassResponse>> Update(int id, ClassRequest request, CancellationToken ct)
    {
        var item = await classes.GetAsync(id, ct);
        if (item is null)
            return NotFound();
        if (!await clubs.ExistsAsync(request.ClubId, ct))
            return ValidationProblem($"Club {request.ClubId} does not exist.");

        item.Apply(request);
        await classes.UpdateAsync(item, ct);
        return item.ToResponse();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await classes.GetAsync(id, ct);
        if (item is null)
            return NotFound();

        await classes.DeleteAsync(item, ct);
        return NoContent();
    }
}

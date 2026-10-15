using FencingClub.Api.Dtos;
using FencingClub.Api.Models;
using FencingClub.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FencingClub.Api.Controllers;

/// <summary>Member records hold personal details, so only staff (Admin, Coach) can see or change them.</summary>
[ApiController]
[Route("api/members")]
[Authorize(Roles = Roles.Staff)]
public class MembersController(IRepository<Member> members, IRepository<Club> clubs) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<MemberResponse>> List(int? clubId, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        var result = await members.ListAsync(clubId is null ? null : m => m.ClubId == clubId, page, pageSize, ct);
        return new PagedResult<MemberResponse>(result.Items.Select(m => m.ToResponse()).ToList(), result.Page, result.PageSize, result.TotalCount);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MemberResponse>> Get(int id, CancellationToken ct)
    {
        var member = await members.GetAsync(id, ct);
        return member is null ? NotFound() : member.ToResponse();
    }

    [HttpPost]
    public async Task<ActionResult<MemberResponse>> Create(MemberRequest request, CancellationToken ct)
    {
        if (!await clubs.ExistsAsync(request.ClubId, ct))
            return ValidationProblem($"Club {request.ClubId} does not exist.");

        var member = new Member();
        member.Apply(request);
        await members.AddAsync(member, ct);
        return CreatedAtAction(nameof(Get), new { id = member.Id }, member.ToResponse());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MemberResponse>> Update(int id, MemberRequest request, CancellationToken ct)
    {
        var member = await members.GetAsync(id, ct);
        if (member is null)
            return NotFound();
        if (!await clubs.ExistsAsync(request.ClubId, ct))
            return ValidationProblem($"Club {request.ClubId} does not exist.");

        member.Apply(request);
        await members.UpdateAsync(member, ct);
        return member.ToResponse();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var member = await members.GetAsync(id, ct);
        if (member is null)
            return NotFound();

        await members.DeleteAsync(member, ct);
        return NoContent();
    }
}

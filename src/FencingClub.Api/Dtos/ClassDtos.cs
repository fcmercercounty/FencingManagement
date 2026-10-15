using System.ComponentModel.DataAnnotations;
using FencingClub.Api.Models;

namespace FencingClub.Api.Dtos;

public record ClassRequest(
    [Range(1, int.MaxValue)] int ClubId,
    [Required, MaxLength(200)] string Title,
    Weapon Weapon,
    DateTime StartsAt,
    [Range(15, 480)] int DurationMinutes,
    [Range(1, 500)] int Capacity,
    [MaxLength(200)] string? CoachName);

public record ClassResponse(
    int Id, int ClubId, string Title, Weapon Weapon, DateTime StartsAt,
    int DurationMinutes, int Capacity, string? CoachName, DateTime CreatedAt, DateTime UpdatedAt);

public static class ClassMapping
{
    public static ClassResponse ToResponse(this FencingClass c) =>
        new(c.Id, c.ClubId, c.Title, c.Weapon, c.StartsAt, c.DurationMinutes, c.Capacity, c.CoachName, c.CreatedAt, c.UpdatedAt);

    public static void Apply(this FencingClass c, ClassRequest r)
    {
        c.ClubId = r.ClubId;
        c.Title = r.Title;
        c.Weapon = r.Weapon;
        c.StartsAt = r.StartsAt;
        c.DurationMinutes = r.DurationMinutes;
        c.Capacity = r.Capacity;
        c.CoachName = r.CoachName;
    }
}

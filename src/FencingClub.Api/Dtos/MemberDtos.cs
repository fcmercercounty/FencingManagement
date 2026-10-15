using System.ComponentModel.DataAnnotations;
using FencingClub.Api.Models;

namespace FencingClub.Api.Dtos;

public record MemberRequest(
    [Range(1, int.MaxValue)] int ClubId,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [EmailAddress, MaxLength(256)] string? Email,
    Weapon PrimaryWeapon,
    bool IsActive = true,
    string? UserId = null);

public record MemberResponse(
    int Id, int ClubId, string FirstName, string LastName, string? Email,
    Weapon PrimaryWeapon, bool IsActive, string? UserId, DateTime CreatedAt, DateTime UpdatedAt);

public static class MemberMapping
{
    public static MemberResponse ToResponse(this Member m) =>
        new(m.Id, m.ClubId, m.FirstName, m.LastName, m.Email, m.PrimaryWeapon, m.IsActive, m.UserId, m.CreatedAt, m.UpdatedAt);

    public static void Apply(this Member m, MemberRequest r)
    {
        m.ClubId = r.ClubId;
        m.FirstName = r.FirstName;
        m.LastName = r.LastName;
        m.Email = r.Email;
        m.PrimaryWeapon = r.PrimaryWeapon;
        m.IsActive = r.IsActive;
        m.UserId = r.UserId;
    }
}

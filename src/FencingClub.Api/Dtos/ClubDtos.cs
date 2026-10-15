using System.ComponentModel.DataAnnotations;
using FencingClub.Api.Models;

namespace FencingClub.Api.Dtos;

public record ClubRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(100)] string? City,
    [EmailAddress, MaxLength(256)] string? Email);

public record ClubResponse(int Id, string Name, string? City, string? Email, DateTime CreatedAt, DateTime UpdatedAt);

public static class ClubMapping
{
    public static ClubResponse ToResponse(this Club c) => new(c.Id, c.Name, c.City, c.Email, c.CreatedAt, c.UpdatedAt);

    public static void Apply(this Club c, ClubRequest r)
    {
        c.Name = r.Name;
        c.City = r.City;
        c.Email = r.Email;
    }
}

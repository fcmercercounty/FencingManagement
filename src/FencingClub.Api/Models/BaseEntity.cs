namespace FencingClub.Api.Models;

/// <summary>Common columns for every domain entity. Timestamps are set by <see cref="Data.AppDbContext"/>.</summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

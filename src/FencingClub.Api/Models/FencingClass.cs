namespace FencingClub.Api.Models;

public class FencingClass : BaseEntity
{
    public int ClubId { get; set; }
    public Club? Club { get; set; }

    public string Title { get; set; } = string.Empty;
    public Weapon Weapon { get; set; }
    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public int Capacity { get; set; } = 12;
    public string? CoachName { get; set; }
}

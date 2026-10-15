namespace FencingClub.Api.Models;

public class Member : BaseEntity
{
    public int ClubId { get; set; }
    public Club? Club { get; set; }

    /// <summary>Optional link to a sign-in account, so members can use the app themselves.</summary>
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public Weapon PrimaryWeapon { get; set; }
    public bool IsActive { get; set; } = true;
}

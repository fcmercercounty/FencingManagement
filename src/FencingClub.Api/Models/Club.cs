namespace FencingClub.Api.Models;

public class Club : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Email { get; set; }

    public List<Member> Members { get; set; } = [];
    public List<FencingClass> Classes { get; set; } = [];
}

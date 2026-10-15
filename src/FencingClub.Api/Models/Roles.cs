namespace FencingClub.Api.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Coach = "Coach";
    public const string Member = "Member";

    public const string Staff = Admin + "," + Coach;

    public static readonly string[] All = [Admin, Coach, Member];
}

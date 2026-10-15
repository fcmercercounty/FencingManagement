using FencingClub.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FencingClub.Api.Data;

/// <summary>
/// Prepares the database at startup: applies migrations (if enabled), creates roles,
/// and optionally creates the first admin account from configuration.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));

        // "Migrate" for SQL Server, "EnsureCreated" for throwaway databases (tests), "None" to manage it yourself.
        switch (config["Database:Initialize"])
        {
            case "Migrate":
                await db.Database.MigrateAsync();
                break;
            case "EnsureCreated":
                await db.Database.EnsureCreatedAsync();
                break;
            default:
                return;
        }

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = config["Seed:AdminEmail"];
        var adminPassword = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            return;

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(adminEmail) is not null)
            return;

        var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, FirstName = "Admin", EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRolesAsync(admin, [Roles.Admin, Roles.Member]);
            logger.LogInformation("Created seed admin account {Email}", adminEmail);
        }
        else
        {
            logger.LogWarning("Could not create seed admin: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}

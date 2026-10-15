using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FencingClub.Api.Data;

/// <summary>
/// Used only by the `dotnet ef` tooling so migrations can be created without a running app or real database.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=FencingClub_DesignTime;Trusted_Connection=True;")
            .Options;
        return new AppDbContext(options);
    }
}

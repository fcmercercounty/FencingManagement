using FencingClub.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FencingClub.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<FencingClass> Classes => Set<FencingClass>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Club>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.City).HasMaxLength(100);
            e.Property(c => c.Email).HasMaxLength(256);
            e.HasIndex(c => c.Name);
        });

        builder.Entity<Member>(e =>
        {
            e.Property(m => m.FirstName).HasMaxLength(100).IsRequired();
            e.Property(m => m.LastName).HasMaxLength(100).IsRequired();
            e.Property(m => m.Email).HasMaxLength(256);
            e.Property(m => m.PrimaryWeapon).HasConversion<string>().HasMaxLength(10);
            e.HasOne(m => m.Club).WithMany(c => c.Members).HasForeignKey(m => m.ClubId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<FencingClass>(e =>
        {
            e.ToTable("Classes");
            e.Property(c => c.Title).HasMaxLength(200).IsRequired();
            e.Property(c => c.CoachName).HasMaxLength(200);
            e.Property(c => c.Weapon).HasConversion<string>().HasMaxLength(10);
            e.HasOne(c => c.Club).WithMany(c => c.Classes).HasForeignKey(c => c.ClubId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(c => new { c.ClubId, c.StartsAt });
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}

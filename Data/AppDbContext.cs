using Microsoft.EntityFrameworkCore;
using PlatformWellSync.Models;

namespace PlatformWellSync.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Platform> Platforms { get; set; }
    public DbSet<Well> Wells { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Platform config
        modelBuilder.Entity<Platform>()
            .Property(p => p.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<Platform>()
            .Property(p => p.Latitude)
            .HasPrecision(18, 6);

        modelBuilder.Entity<Platform>()
            .Property(p => p.Longitude)
            .HasPrecision(18, 6);

        // Well config
        modelBuilder.Entity<Well>()
            .Property(w => w.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<Well>()
            .Property(w => w.Latitude)
            .HasPrecision(18, 6);

        modelBuilder.Entity<Well>()
            .Property(w => w.Longitude)
            .HasPrecision(18, 6);

        // Relationship
        modelBuilder.Entity<Platform>()
            .HasMany(p => p.Wells)
            .WithOne(w => w.Platform)
            .HasForeignKey(w => w.PlatformId);
    }
}
using Microsoft.EntityFrameworkCore;
using DungeonMasterDashboard.Api.GameSystems;

namespace DungeonMasterDashboard.Api.Data;

public sealed class DashboardDbContext(DbContextOptions<DashboardDbContext> options) : DbContext(options)
{
    public DbSet<GameSystemSettings> GameSystemSettings => Set<GameSystemSettings>();

    public DbSet<GameSystemImage> GameSystemImages => Set<GameSystemImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameSystemSettings>(entity =>
        {
            entity.HasKey(s => s.SystemId);
            entity.Property(s => s.SystemId).HasMaxLength(64);
        });

        modelBuilder.Entity<GameSystemImage>(entity =>
        {
            entity.HasKey(i => i.SystemId);
            entity.Property(i => i.SystemId).HasMaxLength(64);
            entity.Property(i => i.ContentType).HasMaxLength(32);
        });
    }
}

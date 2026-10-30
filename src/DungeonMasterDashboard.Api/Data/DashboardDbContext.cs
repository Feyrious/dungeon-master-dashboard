using Microsoft.EntityFrameworkCore;
using DungeonMasterDashboard.Api.GameSystems;
using DungeonMasterDashboard.Api.Worlds;

namespace DungeonMasterDashboard.Api.Data;

public sealed class DashboardDbContext(DbContextOptions<DashboardDbContext> options) : DbContext(options)
{
    public DbSet<GameSystemSettings> GameSystemSettings => Set<GameSystemSettings>();

    public DbSet<GameSystemImage> GameSystemImages => Set<GameSystemImage>();

    public DbSet<World> Worlds => Set<World>();

    public DbSet<WorldImage> WorldImages => Set<WorldImage>();

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

        modelBuilder.Entity<World>(entity =>
        {
            entity.Property(w => w.Name).HasMaxLength(60);
            entity.Property(w => w.Url).HasMaxLength(2000);
            entity.HasData(SeedWorlds);
        });

        modelBuilder.Entity<WorldImage>(entity =>
        {
            entity.HasKey(i => i.WorldId);
            entity.Property(i => i.ContentType).HasMaxLength(32);
            entity.HasOne<World>().WithOne().HasForeignKey<WorldImage>(i => i.WorldId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    // The worlds the app started with. Their links are imported from worlds.env if present
    // (see WorldLinkImport); after that they're edited like any other world.
    private static readonly World[] SeedWorlds =
    [
        new()
        {
            Id = 1, SortOrder = 1, Name = "Curse of Strahd", Tagline = "Gothic horror in the mist-shrouded land of Barovia.",
            Label = "Dungeons & Dragons 5e", WikiSoftware = "foundry-export", BuiltInImage = "curse-of-strahd",
            IsDark = true, Background = "#120a11", Surface = "#1e111c", Text = "#ecdfe8", MutedText = "#a68fa1",
            Accent = "#b0263f", AccentText = "#fff3f5", Border = "#3b2337",
            HeadingFont = "'Times New Roman', Times, serif", BodyFont = "Georgia, serif",
            UpdatedAt = new DateTimeOffset(2026, 10, 30, 0, 0, 0, TimeSpan.Zero),
        },
        new()
        {
            Id = 2, SortOrder = 2, Name = "Rime of the Frostmaiden", Tagline = "Survival under endless night in frozen Icewind Dale.",
            Label = "Dungeons & Dragons 5e", WikiSoftware = "foundry-export", BuiltInImage = "rime-of-the-frostmaiden",
            IsDark = true, Background = "#0b1622", Surface = "#122339", Text = "#e5f0f9", MutedText = "#8eaec8",
            Accent = "#7cc6f2", AccentText = "#07121d", Border = "#24405e",
            HeadingFont = "Georgia, 'Times New Roman', serif", BodyFont = "'Helvetica Neue', Arial, sans-serif",
            UpdatedAt = new DateTimeOffset(2026, 10, 30, 0, 0, 0, TimeSpan.Zero),
        },
    ];
}

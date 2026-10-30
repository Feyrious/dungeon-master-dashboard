using Microsoft.EntityFrameworkCore;
using DungeonMasterDashboard.Api.Data;
using DungeonMasterDashboard.Contracts;

namespace DungeonMasterDashboard.Api.Worlds;

// The worlds that came with the app used to get their links from worlds.env
// (Worlds__{key}__Url). Copies such a link into the world once, if it has none yet.
internal static class WorldLinkImport
{
    public static async Task ImportAsync(DashboardDbContext db, IConfiguration configuration, ILogger logger)
    {
        var worlds = await db.Worlds.Where(w => w.BuiltInImage != null && w.Url == null).ToListAsync();
        foreach (var world in worlds)
        {
            if (configuration[$"Worlds:{world.BuiltInImage}:Url"]?.Trim() is { Length: > 0 } url && SettingsLimits.IsWebAddress(url))
            {
                world.Url = url;
                logger.LogInformation("Imported the link for world {World} from configuration.", world.Name);
            }
        }

        await db.SaveChangesAsync();
    }
}

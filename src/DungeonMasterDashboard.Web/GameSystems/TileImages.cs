using Microsoft.AspNetCore.StaticFiles;

namespace DungeonMasterDashboard.Web.GameSystems;

// Picks the default background image for a game system tile. An image dropped into the
// folder configured as TileImages:Path (named after the system's id, e.g. dnd5e.jpg)
// replaces the built-in illustration in wwwroot/img/tiles. Those custom images may be
// publisher art, so they are served only to signed-in users (see ImageEndpoints).
public sealed class TileImages(IConfiguration configuration, IWebHostEnvironment environment, GameSystemCatalog catalog)
{
    private static readonly string[] Extensions = [".webp", ".jpg", ".jpeg", ".png", ".avif"];
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    public string UrlFor(string tileId)
    {
        var custom = FindCustomImage(tileId);
        return custom is null
            ? $"img/tiles/{tileId}.svg"
            : $"tile-images/{tileId}?v={custom.LastWriteTimeUtc.Ticks}";
    }

    public FileInfo? FindCustomImage(string tileId)
    {
        // Only ids from the catalog are looked up, so a request can't reach other files.
        if (!IsKnownTile(tileId) || configuration["TileImages:Path"] is not { Length: > 0 } path)
        {
            return null;
        }

        var folder = Path.GetFullPath(path, environment.ContentRootPath);
        return Extensions
            .Select(extension => new FileInfo(Path.Combine(folder, tileId + extension)))
            .FirstOrDefault(file => file.Exists);
    }

    public static string ContentTypeFor(FileInfo file) =>
        ContentTypes.TryGetContentType(file.Name, out var contentType) ? contentType : "application/octet-stream";

    private bool IsKnownTile(string tileId) => catalog.Systems.Any(s => s.Id == tileId);
}

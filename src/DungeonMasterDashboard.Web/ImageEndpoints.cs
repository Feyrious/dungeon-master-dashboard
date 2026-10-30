using DungeonMasterDashboard.Web.Api;
using DungeonMasterDashboard.Web.GameSystems;

namespace DungeonMasterDashboard.Web;

// Tile images that aren't public static files: custom images from the tile-images folder and
// images uploaded to the Web API (which the browser can't reach directly). They may be
// publisher art, so only signed-in users get them.
internal static class ImageEndpoints
{
    public static RouteGroupBuilder MapImageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("").RequireAuthorization();

        group.MapGet("/tile-images/{systemId}", (string systemId, TileImages tileImages) =>
            tileImages.FindCustomImage(systemId) is { } file
                ? Results.File(file.FullName, TileImages.ContentTypeFor(file), lastModified: file.LastWriteTimeUtc)
                : Results.NotFound());

        group.MapGet("/system-images/{systemId}", async (
            string systemId, HttpContext context, GameSystemCatalog catalog, ApiClient api, CancellationToken cancellationToken) =>
            catalog.FindSystem(systemId) is null
                ? Results.NotFound()
                : Uploaded(context, await api.GetSystemImageAsync(systemId, context.User, cancellationToken)));

        group.MapGet("/world-images/{worldId:int}", async (
            int worldId, HttpContext context, ApiClient api, CancellationToken cancellationToken) =>
            Uploaded(context, await api.GetWorldImageAsync(worldId, context.User, cancellationToken)));

        return group;
    }

    // Uploaded image URLs carry the upload time, so the image can be cached until it changes.
    private static IResult Uploaded(HttpContext context, (byte[] Data, string ContentType)? image)
    {
        if (image is not { } found)
        {
            return Results.NotFound();
        }

        context.Response.Headers.CacheControl = "private, max-age=31536000, immutable";
        return Results.File(found.Data, found.ContentType);
    }
}

using DungeonMasterDashboard.Contracts;
using DungeonMasterDashboard.Contracts.Worlds;
using DungeonMasterDashboard.Web.Api;
using DungeonMasterDashboard.Web.GameSystems;

namespace DungeonMasterDashboard.Web.Worlds;

// How a world is shown as a tile.
public static class WorldTiles
{
    // Theme for new worlds, matching the neutral look of the site.
    public static GameSystemTheme DefaultTheme { get; } = new(
        IsDark: true,
        Background: "#15171c",
        Surface: "#1f232a",
        Text: "#e8e6e1",
        MutedText: "#a3a8b0",
        Accent: "#d4a24c",
        AccentText: "#15120c",
        Border: "#343a44",
        HeadingFont: "Georgia, 'Times New Roman', serif",
        BodyFont: "'Helvetica Neue', Arial, sans-serif");

    public static string ImageUrl(WorldDto world) => world switch
    {
        { ImageUpdatedAt: { } updatedAt } => $"world-images/{world.Id}?v={updatedAt.UtcTicks}",
        { BuiltInImage: { } key } when SettingsLimits.Id().IsMatch(key) => $"img/tiles/{key}.svg",
        _ => "img/tiles/world.svg",
    };

    public static string LinkText(WorldDto world) =>
        string.IsNullOrWhiteSpace(world.Url) ? "Link not set yet" : $"{WikiSoftware.LinkTextFor(world.WikiSoftware)} ↗";
}

internal static class WorldImagesEndpointRouteBuilderExtensions
{
    // Uploaded world images live in the API, which isn't reachable from the browser,
    // so they're passed through here for signed-in users.
    public static IEndpointConventionBuilder MapWorldImages(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/world-images/{worldId:int}", async (int worldId, HttpContext context, ApiClient api, CancellationToken cancellationToken) =>
            {
                if (await api.GetWorldImageAsync(worldId, context.User, cancellationToken) is not { } image)
                {
                    return Results.NotFound();
                }

                // The URL carries the upload time, so the image can be cached until it changes.
                context.Response.Headers.CacheControl = "private, max-age=31536000, immutable";
                return Results.File(image.Data, image.ContentType);
            })
            .RequireAuthorization();
}

using DungeonMasterDashboard.Contracts.GameSystems;
using DungeonMasterDashboard.Web.Api;

namespace DungeonMasterDashboard.Web.GameSystems;

// A game system as it currently appears: its defaults from code with the saved settings applied.
public sealed record ConfiguredSystem(
    GameSystem Defaults,
    GameSystem System,
    GameSystemSettingsDto? Settings,
    IReadOnlyList<ToolDefinition> AvailableTools,
    IReadOnlyList<ToolDefinition> Tools,
    string ImageUrl)
{
    public bool IsHidden => Settings?.IsHidden ?? false;

    public bool HasUploadedImage => Settings?.ImageUpdatedAt is not null;

    public ToolDefinition? FindTool(string toolId) =>
        Tools.FirstOrDefault(t => string.Equals(t.Id, toolId, StringComparison.OrdinalIgnoreCase));
}

// Combines the game systems defined in code with the changes saved through the API.
// If the API can't be reached the defaults are used, so the site keeps working.
public sealed class ConfiguredGameSystems(
    GameSystemCatalog catalog,
    ApiClient api,
    TileImages tileImages,
    ILogger<ConfiguredGameSystems> logger)
{
    public async Task<IReadOnlyList<ConfiguredSystem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var settings = await TryLoadAsync(() => api.GetAllSystemSettingsAsync(cancellationToken)) ?? [];
        var settingsById = settings.ToDictionary(s => s.SystemId, StringComparer.OrdinalIgnoreCase);
        return catalog.Systems.Select(s => Configure(s, settingsById.GetValueOrDefault(s.Id))).ToList();
    }

    public async Task<ConfiguredSystem?> FindAsync(string systemId, CancellationToken cancellationToken = default)
    {
        if (catalog.FindSystem(systemId) is not { } system)
        {
            return null;
        }

        var settings = await TryLoadAsync(() => api.GetSystemSettingsAsync(system.Id, cancellationToken));
        return Configure(system, settings);
    }

    private ConfiguredSystem Configure(GameSystem defaults, GameSystemSettingsDto? settings)
    {
        var system = settings is null ? defaults : Apply(defaults, settings);
        var available = catalog.ToolsFor(defaults);
        var hidden = settings?.HiddenToolIds.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        var imageUrl = settings?.ImageUpdatedAt is { } updatedAt
            ? $"system-images/{defaults.Id}?v={updatedAt.UtcTicks}"
            : tileImages.UrlFor(defaults.Id);

        return new ConfiguredSystem(defaults, system, settings, available, available.Where(t => !hidden.Contains(t.Id)).ToList(), imageUrl);
    }

    public static GameSystem Apply(GameSystem defaults, GameSystemSettingsDto settings) => defaults with
    {
        Name = settings.Name ?? defaults.Name,
        Tagline = settings.Tagline ?? defaults.Tagline,
        Theme = new GameSystemTheme(
            settings.IsDark ?? defaults.Theme.IsDark,
            settings.Background ?? defaults.Theme.Background,
            settings.Surface ?? defaults.Theme.Surface,
            settings.Text ?? defaults.Theme.Text,
            settings.MutedText ?? defaults.Theme.MutedText,
            settings.Accent ?? defaults.Theme.Accent,
            settings.AccentText ?? defaults.Theme.AccentText,
            settings.Border ?? defaults.Theme.Border,
            settings.HeadingFont ?? defaults.Theme.HeadingFont,
            settings.BodyFont ?? defaults.Theme.BodyFont),
    };

    private async Task<T?> TryLoadAsync<T>(Func<Task<T>> load)
        where T : class?
    {
        try
        {
            return await load();
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Could not load game system settings from the API; using defaults.");
            return default;
        }
    }
}

internal static class SystemImagesEndpointRouteBuilderExtensions
{
    // Uploaded tile images live in the API, which isn't reachable from the browser,
    // so they're passed through here for signed-in users.
    public static IEndpointConventionBuilder MapSystemImages(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/system-images/{systemId}", async (
                string systemId, HttpContext context, GameSystemCatalog catalog, ApiClient api, CancellationToken cancellationToken) =>
            {
                if (catalog.FindSystem(systemId) is null
                    || await api.GetSystemImageAsync(systemId, context.User, cancellationToken) is not { } image)
                {
                    return Results.NotFound();
                }

                // The URL carries the upload time, so the image can be cached until it changes.
                context.Response.Headers.CacheControl = "private, max-age=31536000, immutable";
                return Results.File(image.Data, image.ContentType);
            })
            .RequireAuthorization();
}

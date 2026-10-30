namespace DungeonMasterDashboard.Contracts.Worlds;

// A campaign world: a themed tile on the overview linking to the world's wiki, which can be
// any wiki software hosted anywhere.
public sealed record WorldDto
{
    // Set by the API; ignored when saving.
    public int Id { get; init; }

    public required string Name { get; init; }

    public string? Tagline { get; init; }

    // Small text above the name, e.g. the game system the world is played in.
    public string? Label { get; init; }

    // Address of the world's wiki; the tile isn't clickable without one.
    public string? Url { get; init; }

    // Which wiki software the link points to (an id from the web app's list), used for the link text.
    public string? WikiSoftware { get; init; }

    public required ThemeDto Theme { get; init; }

    // Set by the API; ignored when saving.
    public string? BuiltInImage { get; init; }

    // Set by the API; ignored when saving. Null when the world uses its default image.
    public DateTimeOffset? ImageUpdatedAt { get; init; }
}

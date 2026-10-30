namespace DungeonMasterDashboard.Api.GameSystems;

// Stored changes to a game system defined in the web app. Null means "use the default".
public sealed class GameSystemSettings
{
    public required string SystemId { get; set; }

    public string? Name { get; set; }

    public string? Tagline { get; set; }

    public bool IsHidden { get; set; }

    public List<string> HiddenToolIds { get; set; } = [];

    public bool? IsDark { get; set; }

    public string? Background { get; set; }

    public string? Surface { get; set; }

    public string? Text { get; set; }

    public string? MutedText { get; set; }

    public string? Accent { get; set; }

    public string? AccentText { get; set; }

    public string? Border { get; set; }

    public string? HeadingFont { get; set; }

    public string? BodyFont { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

// Uploaded tile image, kept apart from the settings so listing settings doesn't load images.
public sealed class GameSystemImage
{
    public required string SystemId { get; set; }

    public required byte[] Data { get; set; }

    public required string ContentType { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

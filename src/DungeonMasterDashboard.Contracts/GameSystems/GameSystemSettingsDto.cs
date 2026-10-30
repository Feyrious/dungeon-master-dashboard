namespace DungeonMasterDashboard.Contracts.GameSystems;

// User changes to a game system. Null values mean "use the default defined in code",
// so only what was actually changed is stored.
public sealed record GameSystemSettingsDto
{
    // Set by the API in responses; ignored when saving.
    public string SystemId { get; init; } = "";

    public string? Name { get; init; }

    public string? Tagline { get; init; }

    public bool IsHidden { get; init; }

    public IReadOnlyList<string> HiddenToolIds { get; init; } = [];

    public bool? IsDark { get; init; }

    public string? Background { get; init; }

    public string? Surface { get; init; }

    public string? Text { get; init; }

    public string? MutedText { get; init; }

    public string? Accent { get; init; }

    public string? AccentText { get; init; }

    public string? Border { get; init; }

    public string? HeadingFont { get; init; }

    public string? BodyFont { get; init; }

    // When an image was last uploaded; null when the system uses its default image.
    // Set by the API in responses; ignored when saving.
    public DateTimeOffset? ImageUpdatedAt { get; init; }
}

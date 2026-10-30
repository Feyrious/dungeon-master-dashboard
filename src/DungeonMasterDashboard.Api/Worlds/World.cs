using DungeonMasterDashboard.Api.Images;

namespace DungeonMasterDashboard.Api.Worlds;

// A campaign world tile linking to the world's wiki.
public sealed class World
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Tagline { get; set; }

    public string? Label { get; set; }

    public string? Url { get; set; }

    public string? WikiSoftware { get; set; }

    public bool IsDark { get; set; }

    public string Background { get; set; } = "";

    public string Surface { get; set; } = "";

    public string Text { get; set; } = "";

    public string MutedText { get; set; } = "";

    public string Accent { get; set; } = "";

    public string AccentText { get; set; } = "";

    public string Border { get; set; } = "";

    public string HeadingFont { get; set; } = "";

    public string BodyFont { get; set; } = "";

    // Key of a built-in illustration in the web app (only for the worlds that came with it).
    public string? BuiltInImage { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class WorldImage : IStoredImage
{
    public int WorldId { get; set; }

    public required byte[] Data { get; set; }

    public required string ContentType { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

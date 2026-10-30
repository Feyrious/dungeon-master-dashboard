namespace DungeonMasterDashboard.Api.Worlds;

// A campaign world tile linking to the world's wiki.
public sealed class World
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Tagline { get; set; }

    public string? Label { get; set; }

    public string? Url { get; set; }

    public string? WikiSoftware { get; set; }

    public bool IsDark { get; set; }

    public required string Background { get; set; }

    public required string Surface { get; set; }

    public required string Text { get; set; }

    public required string MutedText { get; set; }

    public required string Accent { get; set; }

    public required string AccentText { get; set; }

    public required string Border { get; set; }

    public required string HeadingFont { get; set; }

    public required string BodyFont { get; set; }

    // Key of a built-in illustration in the web app (only for the worlds that came with it).
    public string? BuiltInImage { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class WorldImage
{
    public int WorldId { get; set; }

    public required byte[] Data { get; set; }

    public required string ContentType { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

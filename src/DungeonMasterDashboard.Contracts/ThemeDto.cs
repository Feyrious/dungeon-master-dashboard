namespace DungeonMasterDashboard.Contracts;

// Colors (#rrggbb) and font lists for a tile and the pages it leads to.
public sealed record ThemeDto(
    bool IsDark,
    string Background,
    string Surface,
    string Text,
    string MutedText,
    string Accent,
    string AccentText,
    string Border,
    string HeadingFont,
    string BodyFont);

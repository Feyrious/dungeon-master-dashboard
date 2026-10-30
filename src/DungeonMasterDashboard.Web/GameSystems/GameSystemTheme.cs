using DungeonMasterDashboard.Contracts;

namespace DungeonMasterDashboard.Web.GameSystems;

// Colors and fonts applied to a system's pages and its tile on the overview grid (also used
// for world tiles).
// Exposed to CSS as --sys-* custom properties.
public sealed record GameSystemTheme(
    bool IsDark,
    string Background,
    string Surface,
    string Text,
    string MutedText,
    string Accent,
    string AccentText,
    string Border,
    string HeadingFont,
    string BodyFont)
{
    public string BootstrapTheme => IsDark ? "dark" : "light";

    public static GameSystemTheme From(ThemeDto theme) => new(
        theme.IsDark, theme.Background, theme.Surface, theme.Text, theme.MutedText,
        theme.Accent, theme.AccentText, theme.Border, theme.HeadingFont, theme.BodyFont);

    public ThemeDto ToDto() => new(IsDark, Background, Surface, Text, MutedText, Accent, AccentText, Border, HeadingFont, BodyFont);

    public string ToCssVariables() =>
        $"--sys-bg:{Background};--sys-surface:{Surface};--sys-text:{Text};--sys-muted:{MutedText};" +
        $"--sys-accent:{Accent};--sys-accent-text:{AccentText};--sys-border:{Border};" +
        $"--sys-heading-font:{HeadingFont};--sys-body-font:{BodyFont};";
}

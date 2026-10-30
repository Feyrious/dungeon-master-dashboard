namespace DungeonMasterDashboard.Web.GameSystems;

// Editable theme values for the game system and world editors.
public sealed class ThemeDraft
{
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

    public static ThemeDraft From(TileTheme theme)
    {
        var draft = new ThemeDraft();
        draft.CopyFrom(theme);
        return draft;
    }

    public void CopyFrom(TileTheme theme)
    {
        IsDark = theme.IsDark;
        Background = theme.Background;
        Surface = theme.Surface;
        Text = theme.Text;
        MutedText = theme.MutedText;
        Accent = theme.Accent;
        AccentText = theme.AccentText;
        Border = theme.Border;
        HeadingFont = theme.HeadingFont;
        BodyFont = theme.BodyFont;
    }

    public TileTheme ToTheme() =>
        new(IsDark, Background, Surface, Text, MutedText, Accent, AccentText, Border, HeadingFont, BodyFont);

    // A comparable summary, used to detect unsaved changes.
    public string Snapshot() => string.Join('|',
        IsDark, string.Join(',', Background, Surface, Text, MutedText, Accent, AccentText, Border).ToLowerInvariant(),
        HeadingFont, BodyFont);
}

public sealed record ThemePreset(string Label, TileTheme Theme);

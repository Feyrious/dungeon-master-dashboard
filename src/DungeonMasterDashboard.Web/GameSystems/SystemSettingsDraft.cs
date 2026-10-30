using System.ComponentModel.DataAnnotations;
using DungeonMasterDashboard.Contracts.GameSystems;

namespace DungeonMasterDashboard.Web.GameSystems;

// The editable form state for a game system's settings.
public sealed class SystemSettingsDraft
{
    [Required(ErrorMessage = "Give the game system a name.")]
    [StringLength(GameSystemSettingsLimits.NameMaxLength, ErrorMessage = "The name can be at most {1} characters.")]
    public string Name { get; set; } = "";

    [StringLength(GameSystemSettingsLimits.TaglineMaxLength, ErrorMessage = "The tagline can be at most {1} characters.")]
    public string Tagline { get; set; } = "";

    public bool ShowOnOverview { get; set; } = true;

    public HashSet<string> HiddenToolIds { get; set; } = [];

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

    public static SystemSettingsDraft From(ConfiguredSystem config)
    {
        var theme = config.System.Theme;
        return new SystemSettingsDraft
        {
            Name = config.System.Name,
            Tagline = config.System.Tagline,
            ShowOnOverview = !config.IsHidden,
            HiddenToolIds = config.Settings?.HiddenToolIds.ToHashSet() ?? [],
            IsDark = theme.IsDark,
            Background = theme.Background,
            Surface = theme.Surface,
            Text = theme.Text,
            MutedText = theme.MutedText,
            Accent = theme.Accent,
            AccentText = theme.AccentText,
            Border = theme.Border,
            HeadingFont = theme.HeadingFont,
            BodyFont = theme.BodyFont,
        };
    }

    public GameSystemTheme ToTheme() =>
        new(IsDark, Background, Surface, Text, MutedText, Accent, AccentText, Border, HeadingFont, BodyFont);

    // Only values that differ from the defaults are sent, so the rest keeps following the code.
    public GameSystemSettingsDto ToDto(GameSystem defaults)
    {
        var theme = defaults.Theme;
        return new GameSystemSettingsDto
        {
            Name = Changed(Name.Trim(), defaults.Name),
            Tagline = Changed(Tagline.Trim(), defaults.Tagline),
            IsHidden = !ShowOnOverview,
            HiddenToolIds = HiddenToolIds.Order().ToList(),
            IsDark = IsDark == theme.IsDark ? null : IsDark,
            Background = Changed(Background, theme.Background),
            Surface = Changed(Surface, theme.Surface),
            Text = Changed(Text, theme.Text),
            MutedText = Changed(MutedText, theme.MutedText),
            Accent = Changed(Accent, theme.Accent),
            AccentText = Changed(AccentText, theme.AccentText),
            Border = Changed(Border, theme.Border),
            HeadingFont = Changed(HeadingFont, theme.HeadingFont),
            BodyFont = Changed(BodyFont, theme.BodyFont),
        };
    }

    // A comparable summary of the form, used to detect unsaved changes.
    public string Snapshot() => string.Join('|',
        Name, Tagline, ShowOnOverview, string.Join(',', HiddenToolIds.Order()), IsDark,
        string.Join(',', Background, Surface, Text, MutedText, Accent, AccentText, Border).ToLowerInvariant(),
        HeadingFont, BodyFont);

    private static string? Changed(string value, string defaultValue) =>
        string.Equals(value, defaultValue, StringComparison.OrdinalIgnoreCase) ? null : value;
}

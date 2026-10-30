using System.ComponentModel.DataAnnotations;
using DungeonMasterDashboard.Contracts;
using DungeonMasterDashboard.Contracts.GameSystems;

namespace DungeonMasterDashboard.Web.GameSystems;

// The editable form state for a game system's settings.
public sealed class SystemSettingsDraft
{
    [Required(ErrorMessage = "Give the game system a name.")]
    [StringLength(SettingsLimits.NameMaxLength, ErrorMessage = "The name can be at most {1} characters.")]
    public string Name { get; set; } = "";

    [StringLength(SettingsLimits.TaglineMaxLength, ErrorMessage = "The tagline can be at most {1} characters.")]
    public string Tagline { get; set; } = "";

    public bool ShowOnOverview { get; set; } = true;

    public HashSet<string> HiddenToolIds { get; set; } = [];

    public ThemeDraft Theme { get; set; } = new();

    public static SystemSettingsDraft From(ConfiguredSystem config) => new()
    {
        Name = config.System.Name,
        Tagline = config.System.Tagline,
        ShowOnOverview = !config.IsHidden,
        HiddenToolIds = config.Settings?.HiddenToolIds.ToHashSet() ?? [],
        Theme = ThemeDraft.From(config.System.Theme),
    };

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
            IsDark = Theme.IsDark == theme.IsDark ? null : Theme.IsDark,
            Background = Changed(Theme.Background, theme.Background),
            Surface = Changed(Theme.Surface, theme.Surface),
            Text = Changed(Theme.Text, theme.Text),
            MutedText = Changed(Theme.MutedText, theme.MutedText),
            Accent = Changed(Theme.Accent, theme.Accent),
            AccentText = Changed(Theme.AccentText, theme.AccentText),
            Border = Changed(Theme.Border, theme.Border),
            HeadingFont = Changed(Theme.HeadingFont, theme.HeadingFont),
            BodyFont = Changed(Theme.BodyFont, theme.BodyFont),
        };
    }

    // A comparable summary of the form, used to detect unsaved changes.
    public string Snapshot() => string.Join('|',
        Name, Tagline, ShowOnOverview, string.Join(',', HiddenToolIds.Order()), Theme.Snapshot());

    private static string? Changed(string value, string defaultValue) =>
        string.Equals(value, defaultValue, StringComparison.OrdinalIgnoreCase) ? null : value;
}

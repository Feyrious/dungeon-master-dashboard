using DungeonMasterDashboard.Contracts;
using static DungeonMasterDashboard.Contracts.SettingsLimits;

namespace DungeonMasterDashboard.Api.Common;

// Collects request validation errors in the shape TypedResults.ValidationProblem expects.
// Theme values end up in a style attribute, so colors and fonts are checked strictly.
internal sealed class ValidationErrors
{
    public Dictionary<string, string[]> Errors { get; } = [];

    public bool Any => Errors.Count > 0;

    public void Add(string field, string message) => Errors[field] = [message];

    // A name must not be blank. Optional names (null = keep the default) are only checked when given.
    public void Name(string? value, bool required)
    {
        if ((required || value is not null) && (string.IsNullOrWhiteSpace(value) || value.Length > NameMaxLength))
        {
            Add("Name", $"Name must be 1 to {NameMaxLength} characters.");
        }
    }

    public void MaxLength(string field, string? value, int maxLength)
    {
        if (value?.Length > maxLength)
        {
            Add(field, $"{field} can be at most {maxLength} characters.");
        }
    }

    public void Color(string field, string? value, bool required = false)
    {
        if ((required || value is not null) && (value is null || !SettingsLimits.Color().IsMatch(value)))
        {
            Add(field, "Use a hex color like #1a2b3c.");
        }
    }

    public void Font(string field, string? value, bool required = false)
    {
        if ((required || value is not null) && (value is null || value.Length > FontMaxLength || !FontFamily().IsMatch(value)))
        {
            Add(field, "Not a valid font list.");
        }
    }

    // A complete theme, as worlds have.
    public void Theme(ThemeDto? theme)
    {
        if (theme is null)
        {
            Add("Theme", "A theme is required.");
            return;
        }

        Color("Theme.Background", theme.Background, required: true);
        Color("Theme.Surface", theme.Surface, required: true);
        Color("Theme.Text", theme.Text, required: true);
        Color("Theme.MutedText", theme.MutedText, required: true);
        Color("Theme.Accent", theme.Accent, required: true);
        Color("Theme.AccentText", theme.AccentText, required: true);
        Color("Theme.Border", theme.Border, required: true);
        Font("Theme.HeadingFont", theme.HeadingFont, required: true);
        Font("Theme.BodyFont", theme.BodyFont, required: true);
    }
}

internal static class TextInput
{
    // Trims text; blank becomes null ("not set").
    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

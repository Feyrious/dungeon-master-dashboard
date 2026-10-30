using System.Text.RegularExpressions;

namespace DungeonMasterDashboard.Contracts.GameSystems;

// Validation rules shared by the API (enforced) and the web editor (shown to the user).
public static partial class GameSystemSettingsLimits
{
    public const int NameMaxLength = 60;
    public const int TaglineMaxLength = 160;
    public const int FontMaxLength = 200;
    public const int ImageMaxBytes = 2 * 1024 * 1024;

    public static readonly IReadOnlyList<string> ImageContentTypes = ["image/webp", "image/jpeg", "image/png"];

    // Values end up in a style attribute, so only plain hex colors are allowed...
    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    public static partial Regex Color();

    // ...and font lists without characters that could close the CSS declaration.
    [GeneratedRegex("^[A-Za-z0-9 ,'\"-]+$")]
    public static partial Regex FontFamily();

    [GeneratedRegex("^[a-z0-9-]{1,64}$")]
    public static partial Regex Id();
}

using System.ComponentModel.DataAnnotations;
using DungeonMasterDashboard.Contracts;
using DungeonMasterDashboard.Contracts.Worlds;
using DungeonMasterDashboard.Web.GameSystems;

namespace DungeonMasterDashboard.Web.Worlds;

// The editable form state for a world.
public sealed class WorldDraft
{
    [Required(ErrorMessage = "Give the world a name.")]
    [StringLength(SettingsLimits.NameMaxLength, ErrorMessage = "The name can be at most {1} characters.")]
    public string Name { get; set; } = "";

    [StringLength(SettingsLimits.TaglineMaxLength, ErrorMessage = "The tagline can be at most {1} characters.")]
    public string Tagline { get; set; } = "";

    [StringLength(SettingsLimits.LabelMaxLength, ErrorMessage = "The label can be at most {1} characters.")]
    public string Label { get; set; } = "";

    [WebAddress]
    public string Url { get; set; } = "";

    public string WikiSoftware { get; set; } = "other";

    public ThemeDraft Theme { get; set; } = new();

    public static WorldDraft New() => new() { Theme = ThemeDraft.From(WorldTiles.DefaultTheme) };

    public static WorldDraft From(WorldDto world) => new()
    {
        Name = world.Name,
        Tagline = world.Tagline ?? "",
        Label = world.Label ?? "",
        Url = world.Url ?? "",
        WikiSoftware = world.WikiSoftware ?? "other",
        Theme = ThemeDraft.From(TileTheme.From(world.Theme)),
    };

    public WorldDto ToDto() => new()
    {
        Name = Name.Trim(),
        Tagline = Blank(Tagline),
        Label = Blank(Label),
        Url = Blank(Url),
        WikiSoftware = WikiSoftware,
        Theme = Theme.ToTheme().ToDto(),
    };

    // A comparable summary of the form, used to detect unsaved changes.
    public string Snapshot() => string.Join('|', Name, Tagline, Label, Url, WikiSoftware, Theme.Snapshot());

    private static string? Blank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

// An optional http(s) address; the API checks the same rule.
public sealed class WebAddressAttribute : ValidationAttribute
{
    public WebAddressAttribute() : base("Use a full web address starting with https:// or http://.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text || string.IsNullOrWhiteSpace(text) || SettingsLimits.IsWebAddress(text.Trim());
}

using DungeonMasterDashboard.Contracts;
using DungeonMasterDashboard.Web.GameSystems;
using DungeonMasterDashboard.Web.GameSystems.Systems;

namespace DungeonMasterDashboard.Tests.Web.GameSystems;

public class ThemeTests
{
    private static readonly TileTheme Dark = DungeonsAndDragons5e.Definition.Theme;
    private static readonly TileTheme Light = FabulaUltima.Definition.Theme;

    [Test]
    public void ToCssVariables_exposes_every_value()
    {
        var css = Dark.ToCssVariables();
        Assert.That(css, Does.Contain($"--sys-bg:{Dark.Background};")
            .And.Contain($"--sys-surface:{Dark.Surface};")
            .And.Contain($"--sys-text:{Dark.Text};")
            .And.Contain($"--sys-muted:{Dark.MutedText};")
            .And.Contain($"--sys-accent:{Dark.Accent};")
            .And.Contain($"--sys-accent-text:{Dark.AccentText};")
            .And.Contain($"--sys-border:{Dark.Border};")
            .And.Contain($"--sys-heading-font:{Dark.HeadingFont};")
            .And.Contain($"--sys-body-font:{Dark.BodyFont};"));
    }

    [Test]
    public void BootstrapTheme_follows_IsDark()
    {
        Assert.That(Dark.BootstrapTheme, Is.EqualTo("dark"));
        Assert.That(Light.BootstrapTheme, Is.EqualTo("light"));
    }

    [Test]
    public void Dto_round_trip_keeps_all_values() =>
        Assert.That(TileTheme.From(Dark.ToDto()), Is.EqualTo(Dark));

    [Test]
    public void ThemeDraft_round_trip_keeps_all_values() =>
        Assert.That(ThemeDraft.From(Light).ToTheme(), Is.EqualTo(Light));

    [Test]
    public void ThemeDraft_CopyFrom_replaces_every_value()
    {
        var draft = ThemeDraft.From(Dark);
        draft.CopyFrom(Light);
        Assert.That(draft.ToTheme(), Is.EqualTo(Light));
    }

    [Test]
    public void ThemeDraft_Snapshot_ignores_color_case_but_sees_changes()
    {
        var draft = ThemeDraft.From(Dark);
        var snapshot = draft.Snapshot();

        draft.Accent = draft.Accent.ToUpperInvariant();
        Assert.That(draft.Snapshot(), Is.EqualTo(snapshot));

        draft.HeadingFont = "Monospace";
        Assert.That(draft.Snapshot(), Is.Not.EqualTo(snapshot));
    }

    [Test]
    public void Every_system_theme_uses_valid_colors_and_fonts()
    {
        foreach (var theme in GameSystemCatalog.Default.Systems.Select(s => s.Theme))
        {
            var dto = theme.ToDto();
            string[] colors = [dto.Background, dto.Surface, dto.Text, dto.MutedText, dto.Accent, dto.AccentText, dto.Border];
            Assert.That(colors, Has.All.Matches(SettingsLimits.Color()));
            Assert.That(new[] { dto.HeadingFont, dto.BodyFont }, Has.All.Matches(SettingsLimits.FontFamily()));
        }
    }

    [Test]
    public void Every_system_theme_is_readable()
    {
        foreach (var system in GameSystemCatalog.Default.Systems)
        {
            var theme = system.Theme;
            Assert.Multiple(() =>
            {
                Assert.That(ColorContrast.Ratio(theme.Text, theme.Background), Is.GreaterThanOrEqualTo(ColorContrast.Readable), system.Id);
                Assert.That(ColorContrast.Ratio(theme.MutedText, theme.Background), Is.GreaterThanOrEqualTo(ColorContrast.Readable), system.Id);
                Assert.That(ColorContrast.Ratio(ColorContrast.Mix(theme.Accent, theme.Text, 0.6), theme.Background),
                    Is.GreaterThanOrEqualTo(ColorContrast.Readable), system.Id);
            });
        }
    }
}

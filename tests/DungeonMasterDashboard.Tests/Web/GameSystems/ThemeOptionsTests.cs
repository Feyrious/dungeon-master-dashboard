using DungeonMasterDashboard.Contracts;
using DungeonMasterDashboard.Web.GameSystems;

namespace DungeonMasterDashboard.Tests.Web.GameSystems;

public class ThemeOptionsTests
{
    [Test]
    public void Contrast_of_black_on_white_is_21() =>
        Assert.That(ColorContrast.Ratio("#000000", "#ffffff"), Is.EqualTo(21).Within(0.01));

    [Test]
    public void Contrast_of_a_color_with_itself_is_1() =>
        Assert.That(ColorContrast.Ratio("#7cc6f2", "#7cc6f2"), Is.EqualTo(1).Within(0.0001));

    [Test]
    public void Contrast_is_the_same_in_both_directions() =>
        Assert.That(ColorContrast.Ratio("#c0392b", "#17110e"), Is.EqualTo(ColorContrast.Ratio("#17110e", "#c0392b")));

    [Test]
    public void Contrast_matches_a_known_value() =>
        // #767676 on white is the classic "just readable" gray at 4.54:1.
        Assert.That(ColorContrast.Ratio("#767676", "#ffffff"), Is.EqualTo(4.54).Within(0.01));

    [TestCase(1.0, "#ff0000")]
    [TestCase(0.0, "#0000ff")]
    [TestCase(0.5, "#800080")]
    public void Mix_blends_like_css_color_mix(double weight, string expected) =>
        Assert.That(ColorContrast.Mix("#ff0000", "#0000ff", weight), Is.EqualTo(expected));

    [Test]
    public void Font_options_are_unique_and_valid()
    {
        Assert.That(FontOptions.All.Select(f => f.Css), Is.Unique.And.All.Matches(SettingsLimits.FontFamily()));
        Assert.That(FontOptions.All.Select(f => f.Label), Is.Unique);
    }

    [Test]
    public void Every_system_font_is_one_of_the_options()
    {
        var css = FontOptions.All.Select(f => f.Css).ToHashSet();
        var fonts = GameSystemCatalog.Default.Systems.SelectMany(s => new[] { s.Theme.HeadingFont, s.Theme.BodyFont });
        Assert.That(fonts, Has.All.Matches<string>(css.Contains));
    }

    [Test]
    public void Including_adds_an_unknown_current_font()
    {
        var options = FontOptions.Including("Papyrus, fantasy");
        Assert.That(options, Has.Count.EqualTo(FontOptions.All.Count + 1));
        Assert.That(options[^1], Is.EqualTo(new FontOption("Current font", "Papyrus, fantasy")));
    }

    [Test]
    public void Including_returns_the_list_unchanged_for_a_known_font() =>
        Assert.That(FontOptions.Including(FontOptions.All[0].Css), Is.SameAs(FontOptions.All));
}

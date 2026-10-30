using Bunit;
using Microsoft.AspNetCore.Components;
using DungeonMasterDashboard.Web.Components.Shared;
using DungeonMasterDashboard.Web.Components.Tools;
using DungeonMasterDashboard.Web.GameSystems;
using DungeonMasterDashboard.Web.GameSystems.Systems;

namespace DungeonMasterDashboard.Tests.Web.Components;

public class SystemTileTests : BunitContext
{
    private static readonly GameSystemTheme Dark = DungeonsAndDragons5e.Definition.Theme;

    [Test]
    public void Renders_a_link_with_the_theme_and_image()
    {
        var tile = Render<SystemTile>(p => p
            .Add(x => x.Theme, Dark)
            .Add(x => x.Name, "Dungeons & Dragons 5e")
            .Add(x => x.Tagline, "Swords")
            .Add(x => x.Label, "Label")
            .Add(x => x.Meta, "4 tools")
            .Add(x => x.ImageUrl, "img/tiles/dnd5e.svg")
            .Add(x => x.Href, "systems/dnd5e")
            .AddUnmatched("target", "_blank"));

        var link = tile.Find("a.system-tile");
        Assert.Multiple(() =>
        {
            Assert.That(link.GetAttribute("href"), Is.EqualTo("systems/dnd5e"));
            Assert.That(link.GetAttribute("target"), Is.EqualTo("_blank"));
            Assert.That(link.GetAttribute("style"), Does.Contain(Dark.ToCssVariables()).And.Contain("--tile-image:url('img/tiles/dnd5e.svg');"));
            Assert.That(link.ClassList, Does.Not.Contain("system-tile-light"));
            Assert.That(tile.Find(".system-tile-label").TextContent, Is.EqualTo("Label"));
            Assert.That(tile.Find(".system-tile-name").TextContent, Is.EqualTo("Dungeons & Dragons 5e"));
            Assert.That(tile.Find(".system-tile-tagline").TextContent, Is.EqualTo("Swords"));
            Assert.That(tile.Find(".system-tile-meta").TextContent, Is.EqualTo("4 tools"));
            Assert.That(tile.FindAll(".system-tile-image"), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Without_a_link_renders_a_plain_tile_and_skips_empty_parts()
    {
        var tile = Render<SystemTile>(p => p
            .Add(x => x.Theme, FabulaUltima.Definition.Theme)
            .Add(x => x.Name, "Fabula Ultima")
            .Add(x => x.Class, "system-tile-disabled"));

        var element = tile.Find("div.system-tile");
        Assert.Multiple(() =>
        {
            Assert.That(tile.FindAll("a"), Is.Empty);
            Assert.That(element.ClassList, Does.Contain("system-tile-light").And.Contain("system-tile-disabled"));
            Assert.That(element.GetAttribute("style"), Does.Not.Contain("--tile-image"));
            Assert.That(tile.FindAll(".system-tile-label, .system-tile-tagline, .system-tile-meta"), Is.Empty);
        });
    }
}

public class ContrastChecksTests : BunitContext
{
    [Test]
    public void Readable_theme_passes_every_check()
    {
        var checks = Render<ContrastChecks>(p => p.Add(x => x.Theme, DungeonsAndDragons5e.Definition.Theme));

        Assert.That(checks.FindAll("li.ok"), Has.Count.EqualTo(4));
        Assert.That(checks.FindAll("li.low"), Is.Empty);
        Assert.That(checks.Markup, Does.Contain("Tool count on tile"));
    }

    [Test]
    public void Unreadable_colors_are_flagged()
    {
        var theme = DungeonsAndDragons5e.Definition.Theme with { Text = "#222222", Accent = "#1c1410" };
        var checks = Render<ContrastChecks>(p => p.Add(x => x.Theme, theme).Add(x => x.MetaLabel, "Link"));

        var low = checks.FindAll("li.low").Select(li => li.TextContent).ToList();
        Assert.That(low, Has.Some.Contains("Text on background").And.Some.Contains("Link on tile"));
        Assert.That(low, Has.All.Contains("hard to read"));
    }
}

public class ThemeEditorTests : BunitContext
{
    [Test]
    public void Changing_a_color_updates_the_theme_and_notifies()
    {
        var draft = ThemeDraft.From(DungeonsAndDragons5e.Definition.Theme);
        var changes = 0;
        var editor = Render<ThemeEditor>(p => p
            .Add(x => x.Theme, draft)
            .Add(x => x.OnChanged, () => changes++));

        editor.FindAll("input[type=color]")[4].Input("#3b8fd9");

        Assert.That(draft.Accent, Is.EqualTo("#3b8fd9"));
        Assert.That(changes, Is.EqualTo(1));
    }

    [Test]
    public void Brightness_and_fonts_can_be_changed()
    {
        var draft = ThemeDraft.From(DungeonsAndDragons5e.Definition.Theme);
        var editor = Render<ThemeEditor>(p => p.Add(x => x.Theme, draft));

        editor.Find("#settings-light").Change(true);
        editor.Find("#settings-heading-font").Change(FontOptions.All[^1].Css);
        editor.Find("#settings-body-font").Change(FontOptions.All[3].Css);

        Assert.Multiple(() =>
        {
            Assert.That(draft.IsDark, Is.False);
            Assert.That(draft.HeadingFont, Is.EqualTo(FontOptions.All[^1].Css));
            Assert.That(draft.BodyFont, Is.EqualTo(FontOptions.All[3].Css));
        });
    }

    [Test]
    public void A_preset_copies_its_whole_theme()
    {
        var draft = ThemeDraft.From(DungeonsAndDragons5e.Definition.Theme);
        var editor = Render<ThemeEditor>(p => p
            .Add(x => x.Theme, draft)
            .Add(x => x.Presets, [new ThemePreset("Kult", Kult.Definition.Theme), new ThemePreset("Fabula", FabulaUltima.Definition.Theme)]));

        editor.Find("#settings-theme-preset").Change("1");

        Assert.That(draft.ToTheme(), Is.EqualTo(FabulaUltima.Definition.Theme));
    }

    [Test]
    public void Preset_list_is_hidden_without_presets() =>
        Assert.That(Render<ThemeEditor>(p => p.Add(x => x.Theme, new ThemeDraft())).FindAll("#settings-theme-preset"), Is.Empty);
}

public class SystemShellTests : BunitContext
{
    [Test]
    public void Side_menu_lists_the_shown_tools_and_links_back()
    {
        AddAuthorization().SetAuthorized("dm@example.com");
        var defaults = DungeonsAndDragons5e.Definition;
        var available = GameSystemCatalog.Default.ToolsFor(defaults);
        var config = new ConfiguredSystem(defaults, defaults with { Name = "D&D 2014" }, null, available,
            available.Where(t => t.Id != "monsters").ToList(), "img/tiles/dnd5e.svg");

        var shell = Render<SystemShell>(p => p
            .Add(x => x.Config, config)
            .AddChildContent("<p id='content'>Tool</p>"));

        Assert.Multiple(() =>
        {
            Assert.That(shell.Find(".system-back").GetAttribute("href"), Is.EqualTo("systems"));
            Assert.That(shell.Find(".system-title").TextContent, Is.EqualTo("D&D 2014"));
            Assert.That(shell.FindAll(".tool-link").Select(l => l.GetAttribute("href")),
                Is.EqualTo(new[] { "systems/dnd5e/npc-names", "systems/dnd5e/dice", "systems/dnd5e/encounters" }));
            Assert.That(shell.Find(".system-shell").GetAttribute("data-bs-theme"), Is.EqualTo("dark"));
            Assert.That(shell.Find("#content").TextContent, Is.EqualTo("Tool"));
            Assert.That(shell.Find(".user-menu-name").TextContent, Is.EqualTo("dm@example.com"));
            Assert.That(shell.Markup, Does.Not.Contain("Edit system"));
        });
    }
}

public class ToolTests : BunitContext
{
    private static IEnumerable<ToolDefinition> Tools => GameSystemCatalog.Default.Tools;

    [TestCaseSource(nameof(Tools))]
    public void Every_tool_renders_for_every_system_it_belongs_to(ToolDefinition tool)
    {
        foreach (var system in GameSystemCatalog.Default.Systems.Where(s => tool.AvailableIn.Includes(s.Id)))
        {
            var rendered = Render<DynamicComponent>(p => p
                .Add(x => x.Type, tool.Component)
                .Add(x => x.Parameters, new Dictionary<string, object> { [nameof(ToolBase.System)] = system }));

            Assert.That(rendered.Find(".tool-placeholder"), Is.Not.Null, $"{tool.Id} in {system.Id}");
        }
    }

    [Test]
    public void Shared_tools_mention_the_system_they_were_opened_from()
    {
        var rendered = Render<NpcNameGenerator>(p => p.Add(x => x.System, TailsOfEquestria.Definition));
        Assert.That(rendered.Markup, Does.Contain("Names will be tailored to Tails of Equestria."));
    }
}

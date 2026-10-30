using System.Net;
using Bunit;
using DungeonMasterDashboard.Contracts.GameSystems;
using DungeonMasterDashboard.Contracts.Worlds;
using DungeonMasterDashboard.Tests.TestSupport;
using DungeonMasterDashboard.Web.Components.Pages;
using DungeonMasterDashboard.Web.Components.Shared;
using DungeonMasterDashboard.Web.Worlds;

namespace DungeonMasterDashboard.Tests.Web.Components;

public class OverviewTests : DialogTestContext
{
    private static readonly WorldDto[] Worlds =
    [
        new() { Id = 1, Name = "Curse of Strahd", Url = "https://example.com/strahd", WikiSoftware = "foundry-export", Theme = WorldTiles.DefaultTheme.ToDto(), BuiltInImage = "curse-of-strahd" },
        new() { Id = 2, Name = "Rime of the Frostmaiden", Theme = WorldTiles.DefaultTheme.ToDto() },
    ];

    private void ApiReturns(GameSystemSettingsDto[]? settings = null, WorldDto[]? worlds = null) =>
        RespondWith(request => request.RequestUri!.AbsolutePath == "/api/worlds"
            ? FakeHttpHandler.Json(worlds ?? Worlds)
            : FakeHttpHandler.Json(settings ?? []));

    [Test]
    public void Shows_worlds_and_game_systems()
    {
        ApiReturns();
        var page = Render<Overview>();

        var worlds = page.FindAll("#worlds-heading ~ * .system-tile, .overview-section:first-of-type .system-tile");
        Assert.Multiple(() =>
        {
            Assert.That(page.FindAll(".overview-heading h2").Select(h => h.TextContent), Is.EqualTo(new[] { "Worlds", "Tools" }));
            Assert.That(page.Find("a[href='https://example.com/strahd']").GetAttribute("target"), Is.EqualTo("_blank"));
            Assert.That(page.Markup, Does.Contain("Open journals ↗").And.Contain("Link not set yet"));
            Assert.That(page.FindAll("a[href^='systems/']").Select(a => a.GetAttribute("href")),
                Is.EqualTo(new[] { "systems/dnd5e", "systems/fabula-ultima", "systems/the-expanse", "systems/kult", "systems/tails-of-equestria" }));
            Assert.That(page.FindAll(".system-tile-edit"), Has.Count.EqualTo(7), "every world and system can be edited");
            Assert.That(page.FindAll(".hidden-systems"), Is.Empty);
        });
    }

    [Test]
    public void Hidden_systems_are_listed_separately()
    {
        ApiReturns(settings: [new GameSystemSettingsDto { SystemId = "kult", IsHidden = true }]);
        var page = Render<Overview>();

        Assert.That(page.Find(".hidden-systems summary").TextContent, Is.EqualTo("Hidden game systems (1)"));
        Assert.That(page.Find(".hidden-systems a.system-tile").GetAttribute("href"), Is.EqualTo("systems/kult"));
    }

    [Test]
    public void An_empty_world_list_explains_the_plus_button()
    {
        ApiReturns(worlds: []);
        Assert.That(Render<Overview>().Find(".overview-empty").TextContent, Does.Contain("Add one with the + button"));
    }

    [Test]
    public void Worlds_show_an_error_when_the_api_is_down()
    {
        RespondWith(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var page = Render<Overview>();

        Assert.That(page.Markup, Does.Contain("Worlds could not be loaded right now."));
        Assert.That(page.FindAll("a[href^='systems/']"), Has.Count.EqualTo(5), "game systems still show with their defaults");
    }

    [Test]
    public void The_plus_button_opens_an_empty_world_dialog()
    {
        ApiReturns();
        var page = Render<Overview>();

        page.Find(".overview-add").Click();

        Assert.That(page.Find("#settings-title").TextContent, Is.EqualTo("Add world"));
        page.Find(".settings-close").Click();
        Assert.That(page.FindAll(".settings-dialog"), Is.Empty);
    }

    [Test]
    public void Pencils_open_the_matching_editor_with_theme_presets()
    {
        ApiReturns();
        var page = Render<Overview>();

        page.Find("button[aria-label='Edit Curse of Strahd']").Click();
        Assert.That(page.Find("#world-name").GetAttribute("value"), Is.EqualTo("Curse of Strahd"));
        page.Find(".settings-close").Click();

        page.Find("button[aria-label='Edit KULT: Divinity Lost']").Click();
        var dialog = page.FindComponent<SystemSettingsDialog>();
        Assert.That(dialog.Instance.Config.Defaults.Id, Is.EqualTo("kult"));
        Assert.That(page.FindAll("#settings-theme-preset option"), Has.Count.EqualTo(1 + 5 + 2), "placeholder, systems and worlds");
    }

    [Test]
    public void Saving_in_an_editor_reloads_the_overview()
    {
        ApiReturns();
        var page = Render<Overview>();
        page.Find("button[aria-label='Edit KULT: Divinity Lost']").Click();
        page.Find("#settings-name").Input("Kult");
        var loadsBefore = Calls.Count(c => c == "GET /api/systems/settings");

        RespondWith(request => request.RequestUri!.AbsolutePath == "/api/worlds"
            ? FakeHttpHandler.Json(Worlds)
            : request.Method == HttpMethod.Put
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : FakeHttpHandler.Json(new[] { new GameSystemSettingsDto { SystemId = "kult", Name = "Kult" } }));
        page.Find("form").Submit();

        Assert.That(Calls.Count(c => c == "GET /api/systems/settings"), Is.EqualTo(loadsBefore + 1));
        Assert.That(page.FindAll(".settings-dialog"), Is.Empty);
        Assert.That(page.Find("a[href='systems/kult'] .system-tile-name").TextContent, Is.EqualTo("Kult"));
    }
}

using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using DungeonMasterDashboard.Contracts.GameSystems;
using DungeonMasterDashboard.Tests.TestSupport;
using DungeonMasterDashboard.Web.Api;
using DungeonMasterDashboard.Web.GameSystems;

namespace DungeonMasterDashboard.Tests.Web.GameSystems;

public class ConfiguredGameSystemsTests
{
    private static ConfiguredGameSystems Create(FakeHttpHandler handler)
    {
        var api = new ApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") },
            new TestAuthenticationStateProvider(TestUsers.Signed()),
            TestUsers.TokenService());
        var tileImages = new TileImages(new ConfigurationBuilder().Build(), new FakeWebHostEnvironment(Path.GetTempPath()), GameSystemCatalog.Default);
        return new ConfiguredGameSystems(GameSystemCatalog.Default, api, tileImages, NullLogger<ConfiguredGameSystems>.Instance);
    }

    [Test]
    public async Task GetAllAsync_uses_the_defaults_when_nothing_was_saved()
    {
        var systems = await Create(FakeHttpHandler.ReturningJson(Array.Empty<GameSystemSettingsDto>())).GetAllAsync();

        Assert.That(systems.Select(s => s.System), Is.EqualTo(GameSystemCatalog.Default.Systems));
        var dnd = systems[0];
        Assert.Multiple(() =>
        {
            Assert.That(dnd.ImageUrl, Is.EqualTo("img/tiles/dnd5e.svg"));
            Assert.That(dnd.IsHidden, Is.False);
            Assert.That(dnd.HasUploadedImage, Is.False);
            Assert.That(dnd.Tools, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public async Task GetAllAsync_applies_saved_settings()
    {
        var settings = new GameSystemSettingsDto
        {
            SystemId = "dnd5e",
            Name = "D&D 2014",
            Accent = "#3b8fd9",
            IsHidden = true,
            HiddenToolIds = ["monsters"],
            ImageUpdatedAt = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero),
        };

        var dnd = (await Create(FakeHttpHandler.ReturningJson(new[] { settings })).GetAllAsync())[0];

        Assert.Multiple(() =>
        {
            Assert.That(dnd.System.Name, Is.EqualTo("D&D 2014"));
            Assert.That(dnd.System.Tagline, Is.EqualTo(dnd.Defaults.Tagline));
            Assert.That(dnd.System.Theme.Accent, Is.EqualTo("#3b8fd9"));
            Assert.That(dnd.System.Theme.Background, Is.EqualTo(dnd.Defaults.Theme.Background));
            Assert.That(dnd.IsHidden, Is.True);
            Assert.That(dnd.AvailableTools.Select(t => t.Id), Does.Contain("monsters"));
            Assert.That(dnd.Tools.Select(t => t.Id), Does.Not.Contain("monsters"));
            Assert.That(dnd.FindTool("monsters"), Is.Null);
            Assert.That(dnd.FindTool("DICE")?.Id, Is.EqualTo("dice"));
            Assert.That(dnd.HasUploadedImage, Is.True);
            Assert.That(dnd.ImageUrl, Is.EqualTo($"system-images/dnd5e?v={settings.ImageUpdatedAt.Value.UtcTicks}"));
        });
    }

    [Test]
    public async Task GetAllAsync_falls_back_to_defaults_when_the_api_fails()
    {
        var systems = await Create(FakeHttpHandler.Returning(HttpStatusCode.InternalServerError)).GetAllAsync();
        Assert.That(systems.Select(s => s.System), Is.EqualTo(GameSystemCatalog.Default.Systems));
    }

    [Test]
    public async Task FindAsync_returns_null_for_unknown_systems_without_calling_the_api()
    {
        var handler = FakeHttpHandler.Returning(HttpStatusCode.OK);
        Assert.That(await Create(handler).FindAsync("nope"), Is.Null);
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task FindAsync_uses_defaults_when_the_system_has_no_settings()
    {
        var handler = FakeHttpHandler.Returning(HttpStatusCode.NotFound);
        var kult = await Create(handler).FindAsync("kult");

        Assert.That(kult!.System, Is.EqualTo(kult.Defaults));
        Assert.That(handler.Requests.Single().Uri.AbsolutePath, Is.EqualTo("/api/systems/kult/settings"));
    }

    [Test]
    public void Apply_keeps_defaults_for_values_that_are_null()
    {
        var defaults = GameSystemCatalog.Default.FindSystem("kult")!;
        var applied = ConfiguredGameSystems.Apply(defaults, new GameSystemSettingsDto { Tagline = "New tagline", IsDark = false, BodyFont = "Georgia, serif" });

        Assert.That(applied, Is.EqualTo(defaults with
        {
            Tagline = "New tagline",
            Theme = defaults.Theme with { IsDark = false, BodyFont = "Georgia, serif" },
        }));
    }
}

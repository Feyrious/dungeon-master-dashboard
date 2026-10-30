using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using DungeonMasterDashboard.Contracts;
using DungeonMasterDashboard.Contracts.Worlds;
using DungeonMasterDashboard.Tests.TestSupport;

namespace DungeonMasterDashboard.Tests.Api;

public class WorldEndpointTests : ApiTestBase
{
    private static readonly ThemeDto Theme = new(
        true, "#101010", "#202020", "#f0f0f0", "#a0a0a0", "#d4a24c", "#101010", "#303030", "Georgia, serif", "'Helvetica Neue', Arial, sans-serif");

    private static WorldDto NewWorld(string name = "Waterdeep") => new()
    {
        Name = name,
        Tagline = "Treasure hunting in the City of Splendors.",
        Label = "Dungeons & Dragons 5e",
        Url = "https://example.com/waterdeep",
        WikiSoftware = "world-anvil",
        Theme = Theme,
    };

    private async Task<List<WorldDto>> GetAllAsync() => (await Client.GetFromJsonAsync<List<WorldDto>>("/api/worlds"))!;

    private async Task<WorldDto> CreateAsync(WorldDto world)
    {
        var response = await Client.PostAsJsonAsync("/api/worlds", world);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        return (await response.Content.ReadFromJsonAsync<WorldDto>())!;
    }

    [Test]
    public async Task The_two_original_worlds_are_seeded()
    {
        var worlds = await GetAllAsync();

        Assert.That(worlds.Select(w => w.Name), Is.EqualTo(new[] { "Curse of Strahd", "Rime of the Frostmaiden" }));
        Assert.Multiple(() =>
        {
            Assert.That(worlds.Select(w => w.BuiltInImage), Is.EqualTo(new[] { "curse-of-strahd", "rime-of-the-frostmaiden" }));
            Assert.That(worlds, Has.All.Property(nameof(WorldDto.Url)).Null);
            Assert.That(worlds, Has.All.Property(nameof(WorldDto.WikiSoftware)).EqualTo("foundry-export"));
            Assert.That(worlds, Has.All.Property(nameof(WorldDto.ImageUpdatedAt)).Null);
        });
    }

    [Test]
    public async Task Created_worlds_are_listed_last_with_their_values()
    {
        var created = await CreateAsync(NewWorld() with { Name = "  Waterdeep  ", Id = 99, BuiltInImage = "kult" });

        var listed = (await GetAllAsync()).Last();
        Assert.Multiple(() =>
        {
            Assert.That(created.Id, Is.Not.EqualTo(99), "ids are assigned by the API");
            Assert.That(listed, Is.EqualTo(created));
            Assert.That(listed.Name, Is.EqualTo("Waterdeep"));
            Assert.That(listed.BuiltInImage, Is.Null, "built-in images can't be set from outside");
            Assert.That(listed.Theme, Is.EqualTo(Theme));
        });
    }

    [Test]
    public async Task Worlds_can_be_updated()
    {
        var created = await CreateAsync(NewWorld());
        var update = NewWorld("Dragon Heist") with { Url = null, Tagline = " ", Theme = Theme with { IsDark = false } };

        var response = await Client.PutAsJsonAsync($"/api/worlds/{created.Id}", update);
        var updated = (await GetAllAsync()).Single(w => w.Id == created.Id);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.Multiple(() =>
        {
            Assert.That(updated.Name, Is.EqualTo("Dragon Heist"));
            Assert.That(updated.Url, Is.Null);
            Assert.That(updated.Tagline, Is.Null);
            Assert.That(updated.Theme.IsDark, Is.False);
        });
    }

    [Test]
    public async Task Unknown_worlds_are_not_found()
    {
        Assert.That((await Client.PutAsJsonAsync("/api/worlds/999", NewWorld())).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await Client.DeleteAsync("/api/worlds/999")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await Client.PutAsync("/api/worlds/999/image", ImageContent(TestImages.Png, "image/png"))).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await Client.GetAsync("/api/worlds/999/image")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [TestCase("javascript:alert(1)")]
    [TestCase("data:text/html,hi")]
    [TestCase("ftp://example.com")]
    [TestCase("not a url")]
    public async Task Only_web_addresses_are_accepted_as_links(string url)
    {
        var response = await Client.PostAsJsonAsync("/api/worlds", NewWorld() with { Url = url });
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(problem!.Errors.Keys, Is.EquivalentTo(new[] { "Url" }));
    }

    [Test]
    public async Task Invalid_worlds_report_every_problem()
    {
        var world = NewWorld() with
        {
            Name = " ",
            Label = new string('l', SettingsLimits.LabelMaxLength + 1),
            Tagline = new string('t', SettingsLimits.TaglineMaxLength + 1),
            WikiSoftware = "Not Valid",
            Theme = Theme with { Accent = "red", HeadingFont = "x;y" },
        };

        var problem = await (await Client.PostAsJsonAsync("/api/worlds", world)).Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.That(problem!.Errors.Keys, Is.EquivalentTo(new[] { "Name", "Label", "Tagline", "WikiSoftware", "Theme.Accent", "Theme.HeadingFont" }));
    }

    [Test]
    public async Task A_world_without_a_theme_is_rejected()
    {
        var json = "{\"name\":\"No theme\"}";
        var response = await Client.PostAsync("/api/worlds", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task World_images_can_be_uploaded_and_removed()
    {
        var world = await CreateAsync(NewWorld());

        var upload = await Client.PutAsync($"/api/worlds/{world.Id}/image", ImageContent(TestImages.Jpeg, "image/jpeg"));
        var listed = (await GetAllAsync()).Single(w => w.Id == world.Id);
        var image = await Client.GetAsync($"/api/worlds/{world.Id}/image");

        Assert.That(upload.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(listed.ImageUpdatedAt, Is.Not.Null);
        Assert.That(await image.Content.ReadAsByteArrayAsync(), Is.EqualTo(TestImages.Jpeg));

        await Client.DeleteAsync($"/api/worlds/{world.Id}/image");
        Assert.That((await GetAllAsync()).Single(w => w.Id == world.Id).ImageUpdatedAt, Is.Null);
        Assert.That((await Client.GetAsync($"/api/worlds/{world.Id}/image")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task World_images_get_the_same_checks_as_system_images()
    {
        var world = await CreateAsync(NewWorld());
        Assert.That((await Client.PutAsync($"/api/worlds/{world.Id}/image", ImageContent(TestImages.Png, "text/plain"))).StatusCode,
            Is.EqualTo(HttpStatusCode.UnsupportedMediaType));
        Assert.That((await Client.PutAsync($"/api/worlds/{world.Id}/image", ImageContent(TestImages.Png, "image/webp"))).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Deleting_a_world_also_deletes_its_image()
    {
        var world = await CreateAsync(NewWorld());
        await Client.PutAsync($"/api/worlds/{world.Id}/image", ImageContent(TestImages.Png, "image/png"));

        Assert.That((await Client.DeleteAsync($"/api/worlds/{world.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        Assert.That((await GetAllAsync()).Select(w => w.Id), Does.Not.Contain(world.Id));
        Assert.That((await Client.GetAsync($"/api/worlds/{world.Id}/image")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Seeded_worlds_can_be_deleted_too()
    {
        await Client.DeleteAsync("/api/worlds/1");
        await Client.DeleteAsync("/api/worlds/2");
        Assert.That(await GetAllAsync(), Is.Empty);
    }
}

public class WorldLinkImportTests : ApiTestBase
{
    protected override IReadOnlyDictionary<string, string> Settings => new Dictionary<string, string>
    {
        ["Worlds:curse-of-strahd:Url"] = "https://example.com/strahd",
        ["Worlds:rime-of-the-frostmaiden:Url"] = "javascript:alert(1)",
    };

    [Test]
    public async Task Valid_old_links_are_imported_into_the_seeded_worlds()
    {
        var worlds = (await Client.GetFromJsonAsync<List<WorldDto>>("/api/worlds"))!;

        Assert.That(worlds.Single(w => w.BuiltInImage == "curse-of-strahd").Url, Is.EqualTo("https://example.com/strahd"));
        Assert.That(worlds.Single(w => w.BuiltInImage == "rime-of-the-frostmaiden").Url, Is.Null, "unsafe links are not imported");
    }

    [Test]
    public async Task Links_set_in_the_app_are_not_overwritten_on_restart()
    {
        var strahd = (await Client.GetFromJsonAsync<List<WorldDto>>("/api/worlds"))!.Single(w => w.Id == 1);
        await Client.PutAsJsonAsync("/api/worlds/1", strahd with { Url = "https://example.com/new-link" });

        // Start the API again on the same database with the same old configuration.
        using var restarted = new ApiFactory(DatabasePath, Settings);
        using var client = restarted.CreateAuthorizedClient();
        var worlds = (await client.GetFromJsonAsync<List<WorldDto>>("/api/worlds"))!;

        Assert.That(worlds.Single(w => w.Id == 1).Url, Is.EqualTo("https://example.com/new-link"));
    }
}

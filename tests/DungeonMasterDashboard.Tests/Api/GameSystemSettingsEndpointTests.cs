using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using DungeonMasterDashboard.Contracts;
using DungeonMasterDashboard.Contracts.GameSystems;
using DungeonMasterDashboard.Tests.TestSupport;

namespace DungeonMasterDashboard.Tests.Api;

public class GameSystemSettingsEndpointTests : ApiTestBase
{
    private Task<HttpResponseMessage> SaveAsync(string systemId, object settings) =>
        Client.PutAsJsonAsync($"/api/systems/{systemId}/settings", settings);

    private async Task<List<GameSystemSettingsDto>> GetAllAsync() =>
        (await Client.GetFromJsonAsync<List<GameSystemSettingsDto>>("/api/systems/settings"))!;

    [Test]
    public async Task Nothing_is_saved_at_first()
    {
        Assert.That(await GetAllAsync(), Is.Empty);
        Assert.That((await Client.GetAsync("/api/systems/dnd5e/settings")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Saved_settings_are_returned()
    {
        var response = await SaveAsync("dnd5e", new GameSystemSettingsDto
        {
            Name = "  D&D 2014  ",
            Tagline = " ",
            Accent = "#3b8fd9",
            IsHidden = true,
            HiddenToolIds = ["monsters", "monsters", "dice"],
            HeadingFont = "Georgia, serif",
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var saved = await Client.GetFromJsonAsync<GameSystemSettingsDto>("/api/systems/dnd5e/settings");
        Assert.Multiple(() =>
        {
            Assert.That(saved!.SystemId, Is.EqualTo("dnd5e"));
            Assert.That(saved.Name, Is.EqualTo("D&D 2014"));
            Assert.That(saved.Tagline, Is.Null, "blank text means use the default");
            Assert.That(saved.Accent, Is.EqualTo("#3b8fd9"));
            Assert.That(saved.Background, Is.Null);
            Assert.That(saved.IsHidden, Is.True);
            Assert.That(saved.HiddenToolIds, Is.EqualTo(new[] { "monsters", "dice" }));
            Assert.That(saved.ImageUpdatedAt, Is.Null);
        });
    }

    [Test]
    public async Task Saving_again_replaces_the_previous_values()
    {
        await SaveAsync("kult", new GameSystemSettingsDto { Name = "First", Accent = "#111111" });
        await SaveAsync("kult", new GameSystemSettingsDto { Name = "Second" });

        var saved = (await GetAllAsync()).Single();
        Assert.That(saved.Name, Is.EqualTo("Second"));
        Assert.That(saved.Accent, Is.Null);
    }

    // Regression: the list used to report a default date for systems without an uploaded image,
    // which made the overview show a missing image after e.g. hiding and showing a system.
    [Test]
    public async Task List_reports_no_image_for_systems_without_an_upload()
    {
        await SaveAsync("dnd5e", new GameSystemSettingsDto { IsHidden = true });
        await SaveAsync("dnd5e", new GameSystemSettingsDto { IsHidden = false });

        Assert.That((await GetAllAsync()).Single().ImageUpdatedAt, Is.Null);
    }

    [TestCase("{\"accent\":\"red\"}", "Accent")]
    [TestCase("{\"background\":\"#12345\"}", "Background")]
    [TestCase("{\"headingFont\":\"Arial;background:url(http://evil)\"}", "HeadingFont")]
    [TestCase("{\"bodyFont\":\"x}\"}", "BodyFont")]
    [TestCase("{\"name\":\"   \"}", "Name")]
    [TestCase("{\"hiddenToolIds\":[\"../x\"]}", "HiddenToolIds")]
    public async Task Invalid_settings_are_rejected(string json, string field)
    {
        var response = await Client.PutAsync("/api/systems/kult/settings", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(problem!.Errors.Keys, Is.EquivalentTo(new[] { field }));
    }

    [Test]
    public async Task Long_names_and_taglines_are_rejected()
    {
        var response = await SaveAsync("kult", new GameSystemSettingsDto
        {
            Name = new string('n', SettingsLimits.NameMaxLength + 1),
            Tagline = new string('t', SettingsLimits.TaglineMaxLength + 1),
        });
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.That(problem!.Errors.Keys, Is.EquivalentTo(new[] { "Name", "Tagline" }));
    }

    [TestCase("Upper")]
    [TestCase("bad%20id")]
    public async Task Invalid_system_ids_are_not_found(string systemId)
    {
        Assert.That((await SaveAsync(systemId, new GameSystemSettingsDto())).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await Client.GetAsync($"/api/systems/{systemId}/settings")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await Client.PutAsync($"/api/systems/{systemId}/image", ImageContent(TestImages.Png, "image/png"))).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }

    [TestCase("image/png")]
    [TestCase("image/jpeg")]
    [TestCase("image/webp")]
    public async Task Uploaded_images_are_returned_as_sent(string contentType)
    {
        var data = contentType switch { "image/png" => TestImages.Png, "image/jpeg" => TestImages.Jpeg, _ => TestImages.WebP };

        var upload = await Client.PutAsync("/api/systems/fabula-ultima/image", ImageContent(data, contentType));
        var image = await Client.GetAsync("/api/systems/fabula-ultima/image");

        Assert.That(upload.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(image.Content.Headers.ContentType?.MediaType, Is.EqualTo(contentType));
        Assert.That(await image.Content.ReadAsByteArrayAsync(), Is.EqualTo(data));
    }

    [Test]
    public async Task Uploading_an_image_alone_creates_a_listing_with_its_date()
    {
        await Client.PutAsync("/api/systems/kult/image", ImageContent(TestImages.Png, "image/png"));

        var listed = (await GetAllAsync()).Single();
        var single = await Client.GetFromJsonAsync<GameSystemSettingsDto>("/api/systems/kult/settings");
        Assert.That(listed.SystemId, Is.EqualTo("kult"));
        Assert.That(listed.ImageUpdatedAt, Is.Not.Null);
        Assert.That(single!.ImageUpdatedAt, Is.EqualTo(listed.ImageUpdatedAt));
    }

    [Test]
    public async Task Images_of_another_type_are_rejected()
    {
        var response = await Client.PutAsync("/api/systems/kult/image", ImageContent(TestImages.Png, "image/gif"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnsupportedMediaType));
    }

    [Test]
    public async Task Files_that_are_not_really_images_are_rejected()
    {
        var response = await Client.PutAsync("/api/systems/kult/image", ImageContent("not an image"u8.ToArray(), "image/png"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Images_over_the_size_limit_are_rejected()
    {
        byte[] large = [.. TestImages.Png, .. new byte[SettingsLimits.ImageMaxBytes]];
        var response = await Client.PutAsync("/api/systems/kult/image", ImageContent(large, "image/png"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.RequestEntityTooLarge));
    }

    [Test]
    public async Task Deleting_the_image_keeps_the_settings()
    {
        await SaveAsync("kult", new GameSystemSettingsDto { Name = "Kult" });
        await Client.PutAsync("/api/systems/kult/image", ImageContent(TestImages.Png, "image/png"));

        Assert.That((await Client.DeleteAsync("/api/systems/kult/image")).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        Assert.That((await Client.GetAsync("/api/systems/kult/image")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var saved = (await GetAllAsync()).Single();
        Assert.That(saved.Name, Is.EqualTo("Kult"));
        Assert.That(saved.ImageUpdatedAt, Is.Null);
    }

    [Test]
    public async Task Reset_removes_settings_and_image()
    {
        await SaveAsync("kult", new GameSystemSettingsDto { Name = "Kult" });
        await Client.PutAsync("/api/systems/kult/image", ImageContent(TestImages.Png, "image/png"));
        await SaveAsync("dnd5e", new GameSystemSettingsDto { Name = "Other" });

        Assert.That((await Client.DeleteAsync("/api/systems/kult/settings")).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        Assert.That((await GetAllAsync()).Select(s => s.SystemId), Is.EqualTo(new[] { "dnd5e" }));
        Assert.That((await Client.GetAsync("/api/systems/kult/image")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}

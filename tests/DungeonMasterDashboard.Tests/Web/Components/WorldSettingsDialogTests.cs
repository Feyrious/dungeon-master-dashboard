using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using DungeonMasterDashboard.Contracts.Worlds;
using DungeonMasterDashboard.Tests.TestSupport;
using DungeonMasterDashboard.Web.Components.Shared;
using DungeonMasterDashboard.Web.Worlds;

namespace DungeonMasterDashboard.Tests.Web.Components;

public class WorldSettingsDialogTests : DialogTestContext
{
    private int closed;
    private int changed;

    private static readonly WorldDto Strahd = new()
    {
        Id = 1,
        Name = "Curse of Strahd",
        Tagline = "Gothic horror",
        Label = "Dungeons & Dragons 5e",
        Url = "https://example.com/strahd",
        WikiSoftware = "foundry-export",
        Theme = WorldTiles.DefaultTheme.ToDto(),
        BuiltInImage = "curse-of-strahd",
    };

    private IRenderedComponent<WorldSettingsDialog> Open(WorldDto? world) =>
        Render<WorldSettingsDialog>(p => p
            .Add(x => x.World, world)
            .Add(x => x.Labels, ["Dungeons & Dragons 5e", "KULT: Divinity Lost"])
            .Add(x => x.OnClose, () => closed++)
            .Add(x => x.OnChanged, () => changed++));

    [Test]
    public void A_new_world_starts_empty_with_the_default_look()
    {
        var dialog = Open(null);

        Assert.Multiple(() =>
        {
            Assert.That(dialog.Find("#settings-title").TextContent, Is.EqualTo("Add world"));
            Assert.That(dialog.Find("button[type=submit]").TextContent.Trim(), Is.EqualTo("Add world"));
            Assert.That(dialog.Find(".settings-preview .system-tile-name").TextContent, Is.EqualTo("New world"));
            Assert.That(dialog.Find(".settings-preview .system-tile-meta").TextContent, Is.EqualTo("Link not set yet"));
            Assert.That(dialog.Find(".settings-preview").InnerHtml, Does.Contain("img/tiles/world.svg"));
            Assert.That(dialog.FindAll("#world-label-suggestions option").Select(o => o.GetAttribute("value")),
                Is.EqualTo(new[] { "Dungeons & Dragons 5e", "KULT: Divinity Lost" }));
            Assert.That(dialog.Markup, Does.Not.Contain("Delete world"));
        });
    }

    [Test]
    public void Wiki_software_choices_include_cc_web_hub_and_set_the_link_text()
    {
        var dialog = Open(null);
        Assert.That(dialog.FindAll("#world-software option").Select(o => o.TextContent), Does.Contain("CC: Web Hub"));

        dialog.Find("#world-url").Input("https://hub.example.com");
        dialog.Find("#world-software").Change("cc-web-hub");

        Assert.That(dialog.Find(".settings-preview .system-tile-meta").TextContent, Is.EqualTo("Open in Web Hub ↗"));
        Assert.That(dialog.Find("a.btn").GetAttribute("href"), Is.EqualTo("https://hub.example.com"), "a valid link can be tested");
    }

    [Test]
    public void Adding_a_world_creates_it_and_uploads_its_image()
    {
        RespondWith(request => request.Method == HttpMethod.Post
            ? FakeHttpHandler.Json(Strahd with { Id = 42 }, HttpStatusCode.Created)
            : new HttpResponseMessage(HttpStatusCode.NoContent));
        var dialog = Open(null);
        dialog.Find("#world-name").Input("Waterdeep");
        dialog.Find("#world-url").Input("https://example.com/waterdeep");
        dialog.InvokeAsync(() => dialog.FindComponent<TileImageEditor>().Instance.ValueChanged.InvokeAsync(
            new TileImageChange.Upload(TestImages.Png, "image/png")));

        dialog.Find("form").Submit();

        Assert.That(Calls, Is.EqualTo(new[] { "POST /api/worlds", "PUT /api/worlds/42/image" }));
        var sent = JsonDocument.Parse(Api.Requests[0].BodyText).RootElement;
        Assert.That(sent.GetProperty("name").GetString(), Is.EqualTo("Waterdeep"));
        Assert.That(sent.GetProperty("url").GetString(), Is.EqualTo("https://example.com/waterdeep"));
        Assert.That((changed, closed), Is.EqualTo((1, 1)));
    }

    [Test]
    public void A_failed_image_upload_does_not_add_the_world_twice()
    {
        var failImage = true;
        RespondWith(request => request.Method == HttpMethod.Post
            ? FakeHttpHandler.Json(Strahd with { Id = 42 }, HttpStatusCode.Created)
            : request.RequestUri!.AbsolutePath.EndsWith("/image") && failImage
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Strahd) });
        var dialog = Open(null);
        dialog.Find("#world-name").Input("Waterdeep");
        dialog.InvokeAsync(() => dialog.FindComponent<TileImageEditor>().Instance.ValueChanged.InvokeAsync(
            new TileImageChange.Upload(TestImages.Png, "image/png")));

        dialog.Find("form").Submit();
        Assert.That(dialog.Markup, Does.Contain("Could not save"));
        failImage = false;
        dialog.Find("form").Submit();

        Assert.That(Calls, Is.EqualTo(new[] { "POST /api/worlds", "PUT /api/worlds/42/image", "PUT /api/worlds/42", "PUT /api/worlds/42/image" }));
    }

    [Test]
    public void Invalid_input_is_not_sent()
    {
        var dialog = Open(null);
        dialog.Find("#world-url").Input("javascript:alert(1)");

        dialog.Find("form").Submit();

        Assert.That(Calls, Is.Empty);
        Assert.That(dialog.FindAll(".validation-message").Select(m => m.TextContent),
            Is.EquivalentTo(new[] { "Give the world a name.", "Use a full web address starting with https:// or http://." }));
        Assert.That(dialog.FindAll("a.btn"), Is.Empty, "no test link for an unsafe address");
    }

    [Test]
    public void Editing_a_world_saves_it_in_place()
    {
        RespondWith(_ => FakeHttpHandler.Json(Strahd));
        var dialog = Open(Strahd);
        Assert.That(dialog.Find("#settings-title").TextContent, Is.EqualTo("Edit world"));
        Assert.That(dialog.Find(".settings-preview").InnerHtml, Does.Contain("img/tiles/curse-of-strahd.svg"));

        dialog.Find("#world-tagline").Input("Mists and vampires");
        dialog.Find("form").Submit();

        Assert.That(Calls, Is.EqualTo(new[] { "PUT /api/worlds/1" }));
        Assert.That(JsonDocument.Parse(Api.Requests[0].BodyText).RootElement.GetProperty("tagline").GetString(), Is.EqualTo("Mists and vampires"));
    }

    [Test]
    public void Deleting_asks_first()
    {
        var dialog = Open(Strahd);

        dialog.FindAll("button").Single(b => b.TextContent == "Delete world").Click();
        Assert.That(Calls, Is.Empty);
        Assert.That(dialog.Markup, Does.Contain("The wiki itself isn't affected."));
        dialog.FindAll("button").Single(b => b.TextContent == "Delete").Click();

        Assert.That(Calls, Is.EqualTo(new[] { "DELETE /api/worlds/1" }));
        Assert.That((changed, closed), Is.EqualTo((1, 1)));
    }

    [Test]
    public void Delete_errors_are_shown()
    {
        RespondWith(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var dialog = Open(Strahd);

        dialog.FindAll("button").Single(b => b.TextContent == "Delete world").Click();
        dialog.FindAll("button").Single(b => b.TextContent == "Delete").Click();

        Assert.That(dialog.Markup, Does.Contain("Could not delete"));
        Assert.That(closed, Is.Zero);
    }

    [Test]
    public void Removing_an_uploaded_image_falls_back_to_the_illustration()
    {
        var dialog = Open(Strahd with { ImageUpdatedAt = DateTimeOffset.UtcNow });
        Assert.That(dialog.Find(".settings-preview").InnerHtml, Does.Contain("world-images/1"));

        dialog.FindAll("button").Single(b => b.TextContent == "Use default image").Click();
        Assert.That(dialog.Find(".settings-preview").InnerHtml, Does.Contain("img/tiles/curse-of-strahd.svg"));

        RespondWith(_ => FakeHttpHandler.Json(Strahd));
        dialog.Find("form").Submit();
        Assert.That(Calls, Is.EqualTo(new[] { "PUT /api/worlds/1", "DELETE /api/worlds/1/image" }));
    }

    [Test]
    public void Unsaved_changes_ask_before_closing()
    {
        ConfirmDialogsAnswer(false);
        var dialog = Open(Strahd);
        dialog.Find("#world-name").Input("Changed");

        dialog.Find(".settings-close").Click();

        Assert.That(closed, Is.Zero);
        JSInterop.VerifyInvoke("confirm");
    }
}

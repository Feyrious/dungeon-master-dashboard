using System.Net;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using DungeonMasterDashboard.Contracts.GameSystems;
using DungeonMasterDashboard.Tests.TestSupport;
using DungeonMasterDashboard.Web.Components.Shared;
using DungeonMasterDashboard.Web.GameSystems;
using DungeonMasterDashboard.Web.GameSystems.Systems;

namespace DungeonMasterDashboard.Tests.Web.Components;

public class SystemSettingsDialogTests : DialogTestContext
{
    private int closed;
    private int changed;

    private static ConfiguredSystem Dnd(GameSystemSettingsDto? settings = null)
    {
        var defaults = DungeonsAndDragons5e.Definition;
        var tools = GameSystemCatalog.Default.ToolsFor(defaults);
        var system = settings is null ? defaults : ConfiguredGameSystems.Apply(defaults, settings);
        var image = settings?.ImageUpdatedAt is null ? "img/tiles/dnd5e.svg" : "system-images/dnd5e?v=1";
        return new ConfiguredSystem(defaults, system, settings, tools, tools, image);
    }

    private IRenderedComponent<SystemSettingsDialog> Open(ConfiguredSystem? config = null) =>
        Render<SystemSettingsDialog>(p => p
            .Add(x => x.Config, config ?? Dnd())
            .Add(x => x.Presets, [new ThemePreset("Kult", Kult.Definition.Theme)])
            .Add(x => x.OnClose, () => closed++)
            .Add(x => x.OnChanged, () => changed++));

    [Test]
    public void Shows_the_current_values_and_a_matching_preview()
    {
        var dialog = Open();

        Assert.Multiple(() =>
        {
            Assert.That(dialog.Find("#settings-title").TextContent, Is.EqualTo("Edit game system"));
            Assert.That(dialog.Find("#settings-name").GetAttribute("value"), Is.EqualTo("Dungeons & Dragons 5e"));
            Assert.That(dialog.FindAll("input[id^=settings-tool-]"), Has.Count.EqualTo(4));
            Assert.That(dialog.Find(".settings-preview .system-tile-name").TextContent, Is.EqualTo("Dungeons & Dragons 5e"));
            Assert.That(dialog.Find(".settings-preview .system-tile-meta").TextContent, Is.EqualTo("4 tools"));
        });
    }

    [Test]
    public void Preview_follows_the_form()
    {
        var dialog = Open();

        dialog.Find("#settings-name").Input("D&D 2014");
        dialog.Find("#settings-tool-monsters").Change(false);
        dialog.Find("#settings-tool-dice").Change(false);
        dialog.Find("#settings-tool-dice").Change(true);

        Assert.That(dialog.Find(".settings-preview .system-tile-name").TextContent, Is.EqualTo("D&D 2014"));
        Assert.That(dialog.Find(".settings-preview .system-tile-meta").TextContent, Is.EqualTo("3 tools"));
    }

    [Test]
    public void Saving_sends_only_changes_then_closes()
    {
        var dialog = Open();
        dialog.Find("#settings-name").Input("D&D 2014");
        dialog.Find("#settings-visible").Change(false);
        dialog.Find("#settings-tool-monsters").Change(false);

        dialog.Find("form").Submit();

        Assert.That(Calls, Is.EqualTo(new[] { "PUT /api/systems/dnd5e/settings" }));
        var sent = JsonDocument.Parse(Api.Requests[0].BodyText).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(sent.GetProperty("name").GetString(), Is.EqualTo("D&D 2014"));
            Assert.That(sent.GetProperty("isHidden").GetBoolean(), Is.True);
            Assert.That(sent.GetProperty("hiddenToolIds")[0].GetString(), Is.EqualTo("monsters"));
            Assert.That(sent.GetProperty("accent").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That((changed, closed), Is.EqualTo((1, 1)));
        });
    }

    [Test]
    public async Task A_new_image_is_uploaded_after_the_settings()
    {
        var dialog = Open();
        var editor = dialog.FindComponent<TileImageEditor>();
        await dialog.InvokeAsync(() => editor.Instance.ValueChanged.InvokeAsync(new TileImageChange.Upload(TestImages.WebP, "image/webp")));

        dialog.Find("form").Submit();

        Assert.That(Calls, Is.EqualTo(new[] { "PUT /api/systems/dnd5e/settings", "PUT /api/systems/dnd5e/image" }));
        Assert.That(Api.Requests[1].Body, Is.EqualTo(TestImages.WebP));
        Assert.That(Api.Requests[1].ContentType, Is.EqualTo("image/webp"));
    }

    [Test]
    public void Using_the_default_image_deletes_the_upload_on_save()
    {
        var dialog = Open(Dnd(new GameSystemSettingsDto { SystemId = "dnd5e", ImageUpdatedAt = DateTimeOffset.UtcNow }));
        Assert.That(dialog.Find(".settings-preview").InnerHtml, Does.Contain("system-images/dnd5e"));

        dialog.FindAll("button").Single(b => b.TextContent == "Use default image").Click();

        Assert.That(dialog.Find(".settings-preview").InnerHtml, Does.Contain("img/tiles/dnd5e.svg"));
        Assert.That(dialog.Markup, Does.Contain("Keep uploaded image"));
        dialog.Find("form").Submit();
        Assert.That(Calls, Is.EqualTo(new[] { "PUT /api/systems/dnd5e/settings", "DELETE /api/systems/dnd5e/image" }));
    }

    [Test]
    public void Api_errors_are_shown_and_the_dialog_stays_open()
    {
        RespondWith(_ => FakeHttpHandler.Json(new { title = "Bad", detail = "Use a hex color." }, HttpStatusCode.BadRequest));
        var dialog = Open();

        dialog.Find("form").Submit();

        Assert.That(dialog.Find(".settings-dialog-footer span.text-danger").TextContent, Is.EqualTo("Could not save: Use a hex color."));
        Assert.That(closed, Is.Zero);
    }

    [Test]
    public void Reset_asks_first_then_removes_the_saved_settings()
    {
        RespondWith(request => request.Method == HttpMethod.Get
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : new HttpResponseMessage(HttpStatusCode.NoContent));
        var dialog = Open(Dnd(new GameSystemSettingsDto { SystemId = "dnd5e", Name = "Renamed" }));
        Assert.That(dialog.Find("#settings-name").GetAttribute("value"), Is.EqualTo("Renamed"));

        dialog.FindAll("button").Single(b => b.TextContent == "Reset to defaults").Click();
        Assert.That(Calls, Is.Empty, "nothing happens before confirming");
        dialog.FindAll("button").Single(b => b.TextContent == "Reset").Click();

        Assert.That(Calls, Is.EqualTo(new[] { "DELETE /api/systems/dnd5e/settings", "GET /api/systems/dnd5e/settings" }));
        Assert.That(dialog.Find("#settings-name").GetAttribute("value"), Is.EqualTo("Dungeons & Dragons 5e"));
        Assert.That((changed, closed), Is.EqualTo((1, 0)), "the dialog stays open after a reset");
    }

    [Test]
    public void Closing_without_changes_does_not_ask()
    {
        var dialog = Open();
        dialog.Find(".settings-close").Click();
        Assert.That(closed, Is.EqualTo(1));
        Assert.That(JSInterop.Invocations.Select(i => i.Identifier), Does.Not.Contain("confirm"));
    }

    [Test]
    public void Closing_with_changes_asks_and_can_be_cancelled()
    {
        ConfirmDialogsAnswer(false);
        var dialog = Open();
        dialog.Find("#settings-name").Input("Changed");

        dialog.Find(".settings-backdrop").Click();
        Assert.That(closed, Is.Zero);

        ConfirmDialogsAnswer(true);
        dialog.Find(".settings-footer-actions button[type=button]").Click();
        Assert.That(closed, Is.EqualTo(1));
        Assert.That(Calls, Is.Empty, "nothing is saved when discarding");
    }

    [Test]
    public async Task Escape_closes_the_dialog()
    {
        var dialog = Open();
        await dialog.InvokeAsync(() => dialog.FindComponent<SettingsDialog>().Instance.Escape());
        Assert.That(closed, Is.EqualTo(1));
    }

    [Test]
    public async Task Escape_while_cropping_only_cancels_the_crop()
    {
        var dialog = Open();
        var editor = dialog.FindComponent<TileImageEditor>();
        await dialog.InvokeAsync(editor.Instance.OnImageLoaded);
        Assert.That(dialog.Find("button[type=submit]").HasAttribute("disabled"), Is.True, "saving waits for the crop");

        await dialog.InvokeAsync(() => dialog.FindComponent<SettingsDialog>().Instance.Escape());

        Assert.That(closed, Is.Zero);
        Assert.That(editor.Instance.IsCropping, Is.False);
        Cropper.VerifyInvoke("cancel");
    }
}

using Bunit;
using DungeonMasterDashboard.Web.Components.Shared;

namespace DungeonMasterDashboard.Tests.Web.Components;

public class TileImageEditorTests : DialogTestContext
{
    private TileImageChange? value;
    private bool? cropping;

    private IRenderedComponent<TileImageEditor> Render(bool hasUploadedImage = false, TileImageChange? current = null) =>
        Render<TileImageEditor>(p => p
            .Add(x => x.PreviewSelector, ".preview")
            .Add(x => x.HasUploadedImage, hasUploadedImage)
            .Add(x => x.Value, current ?? TileImageChange.None)
            .Add(x => x.ValueChanged, change => value = change)
            .Add(x => x.CroppingChanged, isCropping => cropping = isCropping));

    [Test]
    public void Attaches_the_cropper_to_the_preview()
    {
        Render();
        var attach = Cropper.VerifyInvoke("attach");
        Assert.That(attach.Arguments[3], Is.EqualTo(".preview"));
    }

    [Test]
    public void Describes_the_current_image()
    {
        Assert.That(Render().Markup, Does.Contain("Using the default image"));
        Assert.That(Render(hasUploadedImage: true).Markup, Does.Contain("Using an uploaded image"));
        Assert.That(Render(current: new TileImageChange.Upload([1], "image/png")).Markup, Does.Contain("New image ready"));
        Assert.That(Render(hasUploadedImage: true, current: TileImageChange.RemoveUploaded).Markup, Does.Contain("default image will be used"));
    }

    [Test]
    public async Task Loading_an_image_starts_cropping()
    {
        var editor = Render(current: new TileImageChange.Upload([1], "image/png"));
        Assert.That(editor.Find(".cropper").ClassList, Does.Contain("d-none"));

        await editor.InvokeAsync(editor.Instance.OnImageLoaded);

        Assert.Multiple(() =>
        {
            Assert.That(editor.Find(".cropper").ClassList, Does.Not.Contain("d-none"));
            Assert.That(cropping, Is.True);
            Assert.That(value, Is.EqualTo(TileImageChange.None), "an earlier unsaved crop is replaced");
            Cropper.VerifyInvoke("fit");
        });
    }

    [Test]
    public async Task Cancelling_hides_the_cropper()
    {
        var editor = Render();
        await editor.InvokeAsync(editor.Instance.OnImageLoaded);

        editor.FindAll("button").Single(b => b.TextContent == "Cancel").Click();

        Assert.That(cropping, Is.False);
        Assert.That(editor.Find(".cropper").ClassList, Does.Contain("d-none"));
        Cropper.VerifyInvoke("cancel");
    }

    [Test]
    public async Task Crop_errors_are_shown()
    {
        var editor = Render();
        await editor.InvokeAsync(() => editor.Instance.OnCropError("Choose a PNG, JPEG or WebP image."));
        Assert.That(editor.Find("[role=alert]").TextContent, Is.EqualTo("Choose a PNG, JPEG or WebP image."));
    }

    [Test]
    public void Default_image_removes_an_upload_or_discards_a_new_crop()
    {
        Render(hasUploadedImage: true).FindAll("button").Single(b => b.TextContent == "Use default image").Click();
        Assert.That(value, Is.EqualTo(TileImageChange.RemoveUploaded));

        Render(current: new TileImageChange.Upload([1], "image/png")).FindAll("button").Single(b => b.TextContent == "Use default image").Click();
        Assert.That(value, Is.EqualTo(TileImageChange.None));
        Cropper.VerifyInvoke("clearPreview", calledTimes: 2);
    }

    [Test]
    public void Keep_uploaded_image_undoes_a_removal()
    {
        Render(hasUploadedImage: true, current: TileImageChange.RemoveUploaded)
            .FindAll("button").Single(b => b.TextContent == "Keep uploaded image").Click();
        Assert.That(value, Is.EqualTo(TileImageChange.None));
    }
}

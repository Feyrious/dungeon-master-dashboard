using Microsoft.Extensions.Configuration;
using DungeonMasterDashboard.Tests.TestSupport;
using DungeonMasterDashboard.Web.GameSystems;

namespace DungeonMasterDashboard.Tests.Web.GameSystems;

public class TileImagesTests
{
    private TempDirectory directory = null!;

    [SetUp]
    public void SetUp() => directory = new TempDirectory();

    [TearDown]
    public void TearDown() => directory.Dispose();

    private TileImages Create(string? path)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TileImages:Path"] = path })
            .Build();
        return new TileImages(configuration, new FakeWebHostEnvironment(directory.Path), GameSystemCatalog.Default);
    }

    [Test]
    public void UrlFor_uses_the_built_in_illustration_without_a_custom_image() =>
        Assert.That(Create(directory.Path).UrlFor("kult"), Is.EqualTo("img/tiles/kult.svg"));

    [Test]
    public void UrlFor_uses_the_built_in_illustration_when_no_folder_is_configured() =>
        Assert.That(Create(null).UrlFor("kult"), Is.EqualTo("img/tiles/kult.svg"));

    [Test]
    public void UrlFor_uses_a_custom_image_with_a_version()
    {
        File.WriteAllBytes(directory.File("kult.jpg"), TestImages.Jpeg);
        var file = new FileInfo(directory.File("kult.jpg"));

        Assert.That(Create(directory.Path).UrlFor("kult"), Is.EqualTo($"tile-images/kult?v={file.LastWriteTimeUtc.Ticks}"));
    }

    [Test]
    public void FindCustomImage_resolves_relative_folders_against_the_content_root()
    {
        Directory.CreateDirectory(directory.File("images"));
        File.WriteAllBytes(directory.File("images/dnd5e.png"), TestImages.Png);

        Assert.That(Create("images").FindCustomImage("dnd5e")?.Name, Is.EqualTo("dnd5e.png"));
    }

    [Test]
    public void FindCustomImage_prefers_webp_over_other_formats()
    {
        File.WriteAllBytes(directory.File("kult.jpg"), TestImages.Jpeg);
        File.WriteAllBytes(directory.File("kult.webp"), TestImages.WebP);

        Assert.That(Create(directory.Path).FindCustomImage("kult")?.Name, Is.EqualTo("kult.webp"));
    }

    [TestCase("unknown")]
    [TestCase("../secret")]
    [TestCase("kult/../../secret")]
    public void FindCustomImage_ignores_ids_that_are_not_game_systems(string id)
    {
        File.WriteAllBytes(directory.File("unknown.png"), TestImages.Png);
        Assert.That(Create(directory.Path).FindCustomImage(id), Is.Null);
    }

    [TestCase("a.webp", "image/webp")]
    [TestCase("a.jpg", "image/jpeg")]
    [TestCase("a.png", "image/png")]
    [TestCase("a.avif", "image/avif")]
    public void ContentTypeFor_uses_the_extension(string name, string expected) =>
        Assert.That(TileImages.ContentTypeFor(new FileInfo(name)), Is.EqualTo(expected));
}

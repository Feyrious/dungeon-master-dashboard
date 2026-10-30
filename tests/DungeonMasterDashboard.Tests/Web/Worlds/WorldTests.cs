using System.ComponentModel.DataAnnotations;
using DungeonMasterDashboard.Contracts;
using DungeonMasterDashboard.Contracts.Worlds;
using DungeonMasterDashboard.Web.GameSystems;
using DungeonMasterDashboard.Web.Worlds;

namespace DungeonMasterDashboard.Tests.Web.Worlds;

public class WorldTests
{
    private static WorldDto World(Action<WorldDtoBuilder>? configure = null)
    {
        var builder = new WorldDtoBuilder();
        configure?.Invoke(builder);
        return builder.Build();
    }

    [Test]
    public void Wiki_software_ids_are_unique_valid_and_include_cc_web_hub()
    {
        Assert.That(WikiSoftware.All.Select(w => w.Id), Is.Unique.And.All.Matches(SettingsLimits.Id()));
        Assert.That(WikiSoftware.All.Select(w => w.Id), Has.All.Length.LessThanOrEqualTo(SettingsLimits.WikiSoftwareMaxLength));
        Assert.That(WikiSoftware.All.Single(w => w.Id == "cc-web-hub").Name, Is.EqualTo("CC: Web Hub"));
    }

    [TestCase("foundry-export", "Open journals")]
    [TestCase("cc-web-hub", "Open in Web Hub")]
    [TestCase("world-anvil", "Open in World Anvil")]
    [TestCase("other", "Open wiki")]
    [TestCase("not-a-real-id", "Open wiki")]
    [TestCase(null, "Open wiki")]
    public void LinkTextFor_names_the_wiki_software(string? id, string expected) =>
        Assert.That(WikiSoftware.LinkTextFor(id), Is.EqualTo(expected));

    [Test]
    public void LinkText_says_when_no_link_is_set()
    {
        Assert.That(WorldTiles.LinkText(World(w => w.Url = null)), Is.EqualTo("Link not set yet"));
        Assert.That(WorldTiles.LinkText(World(w => w.Url = "  ")), Is.EqualTo("Link not set yet"));
        Assert.That(WorldTiles.LinkText(World(w => { w.Url = "https://x.test"; w.WikiSoftware = "kanka"; })), Is.EqualTo("Open in Kanka ↗"));
    }

    [Test]
    public void ImageUrl_prefers_an_upload_then_a_built_in_illustration()
    {
        var uploaded = new DateTimeOffset(2026, 10, 30, 0, 0, 0, TimeSpan.Zero);
        Assert.Multiple(() =>
        {
            Assert.That(WorldTiles.ImageUrl(World(w => { w.Id = 7; w.ImageUpdatedAt = uploaded; w.BuiltInImage = "curse-of-strahd"; })),
                Is.EqualTo($"world-images/7?v={uploaded.UtcTicks}"));
            Assert.That(WorldTiles.ImageUrl(World(w => w.BuiltInImage = "curse-of-strahd")), Is.EqualTo("img/tiles/curse-of-strahd.svg"));
            Assert.That(WorldTiles.ImageUrl(World()), Is.EqualTo("img/tiles/world.svg"));
            Assert.That(WorldTiles.ImageUrl(World(w => w.BuiltInImage = "../../etc/passwd")), Is.EqualTo("img/tiles/world.svg"));
        });
    }

    [Test]
    public void New_world_draft_uses_the_default_theme_and_other_software()
    {
        var draft = WorldDraft.New();
        Assert.That(draft.Theme.ToTheme(), Is.EqualTo(WorldTiles.DefaultTheme));
        Assert.That(draft.WikiSoftware, Is.EqualTo("other"));
    }

    [Test]
    public void World_draft_round_trips_and_turns_blanks_into_nulls()
    {
        var world = World(w => { w.Tagline = "Tag"; w.Label = "D&D"; w.Url = "https://x.test"; w.WikiSoftware = "notion"; });
        var draft = WorldDraft.From(world);
        Assert.That(draft.ToDto(), Is.EqualTo(world with { Id = 0 }));

        draft.Tagline = "  ";
        draft.Label = "";
        draft.Url = " ";
        draft.Name = "  Trimmed  ";
        var dto = draft.ToDto();
        Assert.Multiple(() =>
        {
            Assert.That(dto.Tagline, Is.Null);
            Assert.That(dto.Label, Is.Null);
            Assert.That(dto.Url, Is.Null);
            Assert.That(dto.Name, Is.EqualTo("Trimmed"));
        });
    }

    [Test]
    public void World_draft_snapshot_sees_theme_and_link_changes()
    {
        var draft = WorldDraft.From(World());
        var snapshot = draft.Snapshot();
        draft.Url = "https://x.test";
        Assert.That(draft.Snapshot(), Is.Not.EqualTo(snapshot));
        draft.Url = "";
        draft.Theme.Border = "#000001";
        Assert.That(draft.Snapshot(), Is.Not.EqualTo(snapshot));
    }

    [TestCase("", true)]
    [TestCase(null, true)]
    [TestCase("https://example.com/wiki", true)]
    [TestCase("  https://example.com  ", true)]
    [TestCase("javascript:alert(1)", false)]
    [TestCase("example.com", false)]
    public void WebAddress_attribute_allows_blank_or_http_addresses(string? value, bool valid) =>
        Assert.That(new WebAddressAttribute().IsValid(value), Is.EqualTo(valid));

    [Test]
    public void World_draft_validation_requires_a_name_and_a_valid_link()
    {
        var draft = WorldDraft.New();
        draft.Url = "javascript:alert(1)";
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(draft, new ValidationContext(draft), results, validateAllProperties: true);

        Assert.That(results.SelectMany(r => r.MemberNames), Is.EquivalentTo(new[] { nameof(WorldDraft.Name), nameof(WorldDraft.Url) }));
    }

    private sealed class WorldDtoBuilder
    {
        public int Id { get; set; } = 1;
        public string? Tagline { get; set; }
        public string? Label { get; set; }
        public string? Url { get; set; }
        public string? WikiSoftware { get; set; }
        public string? BuiltInImage { get; set; }
        public DateTimeOffset? ImageUpdatedAt { get; set; }

        public WorldDto Build() => new()
        {
            Id = Id,
            Name = "World",
            Tagline = Tagline,
            Label = Label,
            Url = Url,
            WikiSoftware = WikiSoftware ?? "other",
            Theme = WorldTiles.DefaultTheme.ToDto(),
            BuiltInImage = BuiltInImage,
            ImageUpdatedAt = ImageUpdatedAt,
        };
    }
}

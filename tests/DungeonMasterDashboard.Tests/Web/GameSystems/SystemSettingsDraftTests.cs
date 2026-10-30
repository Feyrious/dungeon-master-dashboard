using DungeonMasterDashboard.Contracts.GameSystems;
using DungeonMasterDashboard.Web.GameSystems;

namespace DungeonMasterDashboard.Tests.Web.GameSystems;

public class SystemSettingsDraftTests
{
    private static readonly GameSystem Defaults = GameSystemCatalog.Default.FindSystem("dnd5e")!;

    private static ConfiguredSystem Configured(GameSystemSettingsDto? settings = null)
    {
        var tools = GameSystemCatalog.Default.ToolsFor(Defaults);
        var system = settings is null ? Defaults : ConfiguredGameSystems.Apply(Defaults, settings);
        return new ConfiguredSystem(Defaults, system, settings, tools, tools, "img/tiles/dnd5e.svg");
    }

    [Test]
    public void Unchanged_draft_sends_no_overrides()
    {
        var dto = SystemSettingsDraft.From(Configured()).ToDto(Defaults);

        Assert.That(dto, Is.EqualTo(new GameSystemSettingsDto { HiddenToolIds = dto.HiddenToolIds }));
        Assert.That(dto.HiddenToolIds, Is.Empty);
    }

    [Test]
    public void Only_changed_values_are_sent()
    {
        var draft = SystemSettingsDraft.From(Configured());
        draft.Name = "  D&D 2014  ";
        draft.Theme.Accent = "#3b8fd9";
        draft.Theme.IsDark = false;
        draft.ShowOnOverview = false;
        draft.HiddenToolIds.UnionWith(["monsters", "dice"]);

        var dto = draft.ToDto(Defaults);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Name, Is.EqualTo("D&D 2014"));
            Assert.That(dto.Tagline, Is.Null);
            Assert.That(dto.Accent, Is.EqualTo("#3b8fd9"));
            Assert.That(dto.Background, Is.Null);
            Assert.That(dto.IsDark, Is.False);
            Assert.That(dto.IsHidden, Is.True);
            Assert.That(dto.HiddenToolIds, Is.EqualTo(new[] { "dice", "monsters" }));
        });
    }

    [Test]
    public void Colors_that_only_differ_in_case_count_as_unchanged()
    {
        var draft = SystemSettingsDraft.From(Configured());
        draft.Theme.Accent = Defaults.Theme.Accent.ToUpperInvariant();
        Assert.That(draft.ToDto(Defaults).Accent, Is.Null);
    }

    [Test]
    public void From_reads_the_saved_settings()
    {
        var draft = SystemSettingsDraft.From(Configured(new GameSystemSettingsDto { Name = "Renamed", IsHidden = true, HiddenToolIds = ["dice"] }));

        Assert.Multiple(() =>
        {
            Assert.That(draft.Name, Is.EqualTo("Renamed"));
            Assert.That(draft.ShowOnOverview, Is.False);
            Assert.That(draft.HiddenToolIds, Is.EquivalentTo(new[] { "dice" }));
        });
    }

    [Test]
    public void Snapshot_changes_when_something_is_edited()
    {
        var draft = SystemSettingsDraft.From(Configured());
        var snapshot = draft.Snapshot();

        draft.HiddenToolIds.Add("dice");
        Assert.That(draft.Snapshot(), Is.Not.EqualTo(snapshot));

        draft.HiddenToolIds.Clear();
        Assert.That(draft.Snapshot(), Is.EqualTo(snapshot));

        draft.Name = Defaults.Name.ToUpperInvariant();
        Assert.That(draft.Snapshot(), Is.Not.EqualTo(snapshot), "renaming only by case is still a change");
    }
}

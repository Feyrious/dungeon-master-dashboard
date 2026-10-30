using DungeonMasterDashboard.Web.Components.Tools;
using DungeonMasterDashboard.Web.GameSystems;
using DungeonMasterDashboard.Web.GameSystems.Systems;

namespace DungeonMasterDashboard.Tests.Web.GameSystems;

public class GameSystemCatalogTests
{
    private static readonly GameSystemCatalog Catalog = GameSystemCatalog.Default;

    [Test]
    public void Default_contains_the_five_game_systems_in_order() =>
        Assert.That(Catalog.Systems.Select(s => s.Id), Is.EqualTo(new[] { "dnd5e", "fabula-ultima", "the-expanse", "kult", "tails-of-equestria" }));

    [Test]
    public void Default_ids_are_valid_slugs_and_tools_are_unique()
    {
        Assert.That(Catalog.Systems.Select(s => s.Id), Has.All.Matches(DungeonMasterDashboard.Contracts.SettingsLimits.Id()));
        Assert.That(Catalog.Tools.Select(t => t.Id), Is.Unique.And.All.Matches(DungeonMasterDashboard.Contracts.SettingsLimits.Id()));
    }

    [Test]
    public void Every_tool_component_inherits_ToolBase() =>
        Assert.That(Catalog.Tools.Select(t => t.Component), Has.All.Matches<Type>(t => t.IsSubclassOf(typeof(ToolBase))));

    [TestCase("dnd5e")]
    [TestCase("DnD5E")]
    public void FindSystem_ignores_case(string id) =>
        Assert.That(Catalog.FindSystem(id)?.Name, Is.EqualTo("Dungeons & Dragons 5e"));

    [Test]
    public void FindSystem_returns_null_for_unknown_ids() =>
        Assert.That(Catalog.FindSystem("nope"), Is.Null);

    [Test]
    public void ToolsFor_includes_shared_tools_and_the_systems_own_tools()
    {
        var dnd = Catalog.FindSystem("dnd5e")!;
        var kult = Catalog.FindSystem("kult")!;

        Assert.Multiple(() =>
        {
            Assert.That(Catalog.ToolsFor(dnd).Select(t => t.Id), Is.EqualTo(new[] { "npc-names", "dice", "encounters", "monsters" }));
            Assert.That(Catalog.ToolsFor(kult).Select(t => t.Id), Is.EqualTo(new[] { "npc-names", "dice" }));
        });
    }

    [Test]
    public void Constructor_rejects_duplicate_tool_ids()
    {
        var tool = NpcNameGenerator.Definition;
        var error = Assert.Throws<InvalidOperationException>(() => new GameSystemCatalog([DungeonsAndDragons5e.Definition], [tool, tool with { Name = "Copy" }]));
        Assert.That(error!.Message, Does.Contain("Duplicate tool id 'npc-names'"));
    }

    [Test]
    public void Constructor_rejects_tools_for_unknown_systems()
    {
        var error = Assert.Throws<InvalidOperationException>(() => new GameSystemCatalog([Kult.Definition], [MonsterBrowser.Definition]));
        Assert.That(error!.Message, Does.Contain("unknown game system 'dnd5e'"));
    }

    [Test]
    public void ToolAvailability_AllSystems_includes_any_system() =>
        Assert.That(ToolAvailability.AllSystems.Includes("anything"), Is.True);

    [Test]
    public void ToolAvailability_Only_includes_just_the_listed_systems()
    {
        var availability = ToolAvailability.Only("dnd5e", "kult");
        Assert.Multiple(() =>
        {
            Assert.That(availability.Includes("kult"), Is.True);
            Assert.That(availability.Includes("fabula-ultima"), Is.False);
        });
    }
}

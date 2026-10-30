using DungeonMasterDashboard.Web.Components.Tools;
using DungeonMasterDashboard.Web.GameSystems.Systems;
using DungeonMasterDashboard.Web.GameSystems.Worlds;

namespace DungeonMasterDashboard.Web.GameSystems;

// The single place where game systems, tools and worlds are registered.
// To add a system: create a file in GameSystems/Systems and add it to the systems list.
// To add a tool: create a component in Components/Tools and add its Definition to the tools list.
// To add a world: create a file in GameSystems/Worlds and add it to the worlds list.
public sealed class GameSystemCatalog
{
    public static GameSystemCatalog Default { get; } = new(
        systems:
        [
            DungeonsAndDragons5e.Definition,
            FabulaUltima.Definition,
            TheExpanse.Definition,
            Kult.Definition,
            TailsOfEquestria.Definition,
        ],
        tools:
        [
            NpcNameGenerator.Definition,
            DiceRoller.Definition,
            EncounterBuilder.Definition,
            MonsterBrowser.Definition,
        ],
        worlds:
        [
            CurseOfStrahd.Definition,
            RimeOfTheFrostmaiden.Definition,
        ]);

    private readonly Dictionary<string, GameSystem> systemsById;

    public GameSystemCatalog(IReadOnlyList<GameSystem> systems, IReadOnlyList<ToolDefinition> tools, IReadOnlyList<World> worlds)
    {
        systemsById = systems.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        Systems = systems;
        Tools = tools;
        Worlds = worlds;

        var duplicateTool = tools.GroupBy(t => t.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicateTool is not null)
        {
            throw new InvalidOperationException($"Duplicate tool id '{duplicateTool.Key}'.");
        }

        foreach (var tool in tools)
        {
            var unknown = tool.AvailableIn.SystemIds?.FirstOrDefault(id => !systemsById.ContainsKey(id));
            if (unknown is not null)
            {
                throw new InvalidOperationException($"Tool '{tool.Id}' refers to unknown game system '{unknown}'.");
            }
        }

        var duplicateWorld = worlds.GroupBy(w => w.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicateWorld is not null)
        {
            throw new InvalidOperationException($"Duplicate world id '{duplicateWorld.Key}'.");
        }

        var orphanWorld = worlds.FirstOrDefault(w => !systemsById.ContainsKey(w.SystemId));
        if (orphanWorld is not null)
        {
            throw new InvalidOperationException($"World '{orphanWorld.Id}' refers to unknown game system '{orphanWorld.SystemId}'.");
        }
    }

    public IReadOnlyList<GameSystem> Systems { get; }

    public IReadOnlyList<ToolDefinition> Tools { get; }

    public IReadOnlyList<World> Worlds { get; }

    public GameSystem? FindSystem(string systemId) => systemsById.GetValueOrDefault(systemId);

    public IReadOnlyList<ToolDefinition> ToolsFor(GameSystem system) =>
        Tools.Where(t => t.AvailableIn.Includes(system.Id)).ToList();

    public ToolDefinition? FindTool(GameSystem system, string toolId) =>
        Tools.FirstOrDefault(t => string.Equals(t.Id, toolId, StringComparison.OrdinalIgnoreCase) && t.AvailableIn.Includes(system.Id));
}

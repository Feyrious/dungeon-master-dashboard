using DungeonMasterDashboard.Web.Components.Tools;
using DungeonMasterDashboard.Web.GameSystems.Systems;

namespace DungeonMasterDashboard.Web.GameSystems;

// The single place where game systems and tools are registered.
// To add a system: create a file in GameSystems/Systems and add it to the systems list.
// To add a tool: create a component in Components/Tools and add its Definition to the tools list.
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
        ]);

    private readonly Dictionary<string, GameSystem> systemsById;

    public GameSystemCatalog(IReadOnlyList<GameSystem> systems, IReadOnlyList<ToolDefinition> tools)
    {
        systemsById = systems.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        Systems = systems;
        Tools = tools;

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
    }

    public IReadOnlyList<GameSystem> Systems { get; }

    public IReadOnlyList<ToolDefinition> Tools { get; }

    public GameSystem? FindSystem(string systemId) => systemsById.GetValueOrDefault(systemId);

    public IReadOnlyList<ToolDefinition> ToolsFor(GameSystem system) =>
        Tools.Where(t => t.AvailableIn.Includes(system.Id)).ToList();
}

namespace DungeonMasterDashboard.Web.GameSystems;

// Describes a tool page. Each tool is a Razor component under Components/Tools that
// declares its own Definition and is registered in GameSystemCatalog.
public sealed record ToolDefinition(
    string Id,
    string Name,
    string Description,
    string Icon,
    Type Component,
    ToolAvailability AvailableIn);

// Which game systems a tool shows up in.
public sealed record ToolAvailability(IReadOnlySet<string>? SystemIds)
{
    public static ToolAvailability AllSystems { get; } = new((IReadOnlySet<string>?)null);

    public static ToolAvailability Only(params string[] systemIds) => new(systemIds.ToHashSet());

    public bool Includes(string systemId) => SystemIds is null || SystemIds.Contains(systemId);
}

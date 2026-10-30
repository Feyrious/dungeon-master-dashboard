namespace DungeonMasterDashboard.Web.GameSystems;

public static class ToolCount
{
    // "1 tool", "3 tools"; shown on game system tiles.
    public static string Text(int count) => $"{count} {(count == 1 ? "tool" : "tools")}";
}

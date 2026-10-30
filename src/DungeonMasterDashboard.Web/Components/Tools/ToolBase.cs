using Microsoft.AspNetCore.Components;
using DungeonMasterDashboard.Web.GameSystems;

namespace DungeonMasterDashboard.Web.Components.Tools;

// Base class for tool pages. The hosting page passes in the game system the tool is
// opened from, so a tool shared between systems can adapt to it.
public abstract class ToolBase : ComponentBase
{
    [Parameter, EditorRequired]
    public GameSystem System { get; set; } = default!;
}

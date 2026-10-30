namespace DungeonMasterDashboard.Web.GameSystems;

// A game system groups the tools used for it. Each system is defined in its own file
// under GameSystems/Systems and registered in GameSystemCatalog.
public sealed record GameSystem(string Id, string Name, string Tagline, GameSystemTheme Theme);

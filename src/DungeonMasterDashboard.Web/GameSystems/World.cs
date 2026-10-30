namespace DungeonMasterDashboard.Web.GameSystems;

// A campaign world whose journals are exported from Foundry VTT to their own web page.
// Each world is defined in its own file under GameSystems/Worlds and registered in
// GameSystemCatalog. The address of the exported site comes from configuration
// (Worlds:{Id}:Url) so it can change without a rebuild.
public sealed record World(string Id, string Name, string Tagline, string SystemId, GameSystemTheme Theme);

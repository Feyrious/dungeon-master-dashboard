namespace DungeonMasterDashboard.Web.Worlds;

// Wiki software a world can link to. Only used for the link text on the tile; the link
// itself can point to any site.
public sealed record WikiSoftware(string Id, string Name, string LinkText)
{
    public const string DefaultLinkText = "Open wiki";

    public static IReadOnlyList<WikiSoftware> All { get; } =
    [
        new("foundry-export", "Foundry VTT journal export", "Open journals"),
        new("world-anvil", "World Anvil", "Open in World Anvil"),
        new("kanka", "Kanka", "Open in Kanka"),
        new("legendkeeper", "LegendKeeper", "Open in LegendKeeper"),
        new("obsidian-publish", "Obsidian Publish", "Open in Obsidian"),
        new("notion", "Notion", "Open in Notion"),
        new("mediawiki", "MediaWiki / Fandom", DefaultLinkText),
        new("wikijs", "Wiki.js", DefaultLinkText),
        new("dokuwiki", "DokuWiki", DefaultLinkText),
        new("other", "Other / any website", DefaultLinkText),
    ];

    public static string LinkTextFor(string? id) => All.FirstOrDefault(w => w.Id == id)?.LinkText ?? DefaultLinkText;
}

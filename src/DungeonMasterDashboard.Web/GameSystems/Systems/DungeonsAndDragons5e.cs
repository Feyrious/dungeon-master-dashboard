namespace DungeonMasterDashboard.Web.GameSystems.Systems;

public static class DungeonsAndDragons5e
{
    public const string Id = "dnd5e";

    public static GameSystem Definition { get; } = new(
        Id,
        "Dungeons & Dragons 5e",
        "Swords, sorcery and dragons.",
        new GameSystemTheme(
            IsDark: true,
            Background: "#17110e",
            Surface: "#251b16",
            Text: "#f1e6d2",
            MutedText: "#b8a68c",
            Accent: "#c0392b",
            AccentText: "#fff4e6",
            Border: "#4a3528",
            HeadingFont: "Georgia, 'Times New Roman', serif",
            BodyFont: "Georgia, 'Times New Roman', serif"));
}

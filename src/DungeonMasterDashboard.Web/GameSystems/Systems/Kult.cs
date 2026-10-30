namespace DungeonMasterDashboard.Web.GameSystems.Systems;

public static class Kult
{
    public const string Id = "kult";

    public static GameSystem Definition { get; } = new(
        Id,
        "KULT: Divinity Lost",
        "Contemporary horror behind the veil of reality.",
        new TileTheme(
            IsDark: true,
            Background: "#0b0909",
            Surface: "#161111",
            Text: "#d9d0c7",
            MutedText: "#8c7f74",
            Accent: "#9e1b1b",
            AccentText: "#f5ece4",
            Border: "#352626",
            HeadingFont: "'Times New Roman', Times, serif",
            BodyFont: "Georgia, serif"));
}

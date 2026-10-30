namespace DungeonMasterDashboard.Web.GameSystems.Systems;

public static class FabulaUltima
{
    public const string Id = "fabula-ultima";

    public static GameSystem Definition { get; } = new(
        Id,
        "Fabula Ultima",
        "Heroic adventures in the style of classic JRPGs.",
        new TileTheme(
            IsDark: false,
            Background: "#f4f1ff",
            Surface: "#ffffff",
            Text: "#2b2350",
            MutedText: "#6b628f",
            Accent: "#6c3fe0",
            AccentText: "#ffffff",
            Border: "#d9d0f5",
            HeadingFont: "'Trebuchet MS', 'Segoe UI', sans-serif",
            BodyFont: "'Segoe UI', 'Helvetica Neue', Arial, sans-serif"));
}

namespace DungeonMasterDashboard.Web.GameSystems.Systems;

public static class TheExpanse
{
    public const string Id = "the-expanse";

    public static GameSystem Definition { get; } = new(
        Id,
        "The Expanse",
        "Hard sci-fi across a divided solar system.",
        new TileTheme(
            IsDark: true,
            Background: "#0a1017",
            Surface: "#111b26",
            Text: "#d6e4f0",
            MutedText: "#7f97ad",
            Accent: "#f0a030",
            AccentText: "#0a1017",
            Border: "#22364a",
            HeadingFont: "'DejaVu Sans Mono', Consolas, monospace",
            BodyFont: "'Helvetica Neue', Arial, sans-serif"));
}

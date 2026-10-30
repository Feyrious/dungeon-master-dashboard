namespace DungeonMasterDashboard.Web.GameSystems.Systems;

public static class TailsOfEquestria
{
    public const string Id = "tails-of-equestria";

    public static GameSystem Definition { get; } = new(
        Id,
        "Tails of Equestria",
        "Friendship and adventure for every age.",
        new GameSystemTheme(
            IsDark: false,
            Background: "#fff5fa",
            Surface: "#ffffff",
            Text: "#4a3557",
            MutedText: "#8a7398",
            Accent: "#d9579d",
            AccentText: "#ffffff",
            Border: "#f3d3e6",
            HeadingFont: "'Trebuchet MS', 'Comic Sans MS', sans-serif",
            BodyFont: "'Trebuchet MS', 'Segoe UI', sans-serif"));
}

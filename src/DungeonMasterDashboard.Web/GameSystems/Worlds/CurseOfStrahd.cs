using DungeonMasterDashboard.Web.GameSystems.Systems;

namespace DungeonMasterDashboard.Web.GameSystems.Worlds;

public static class CurseOfStrahd
{
    public const string Id = "curse-of-strahd";

    public static World Definition { get; } = new(
        Id,
        "Curse of Strahd",
        "Gothic horror in the mist-shrouded land of Barovia.",
        DungeonsAndDragons5e.Id,
        new GameSystemTheme(
            IsDark: true,
            Background: "#120a11",
            Surface: "#1e111c",
            Text: "#ecdfe8",
            MutedText: "#a68fa1",
            Accent: "#b0263f",
            AccentText: "#fff3f5",
            Border: "#3b2337",
            HeadingFont: "'Times New Roman', Times, serif",
            BodyFont: "Georgia, serif"));
}

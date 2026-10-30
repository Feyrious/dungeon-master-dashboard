using DungeonMasterDashboard.Web.GameSystems.Systems;

namespace DungeonMasterDashboard.Web.GameSystems.Worlds;

public static class RimeOfTheFrostmaiden
{
    public const string Id = "rime-of-the-frostmaiden";

    public static World Definition { get; } = new(
        Id,
        "Rime of the Frostmaiden",
        "Survival under endless night in frozen Icewind Dale.",
        DungeonsAndDragons5e.Id,
        new GameSystemTheme(
            IsDark: true,
            Background: "#0b1622",
            Surface: "#122339",
            Text: "#e5f0f9",
            MutedText: "#8eaec8",
            Accent: "#7cc6f2",
            AccentText: "#07121d",
            Border: "#24405e",
            HeadingFont: "Georgia, 'Times New Roman', serif",
            BodyFont: "'Helvetica Neue', Arial, sans-serif"));
}

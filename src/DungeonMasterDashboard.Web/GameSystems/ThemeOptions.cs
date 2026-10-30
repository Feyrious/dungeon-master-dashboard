using System.Globalization;

namespace DungeonMasterDashboard.Web.GameSystems;

public sealed record FontOption(string Label, string Css);

// Fonts offered in the system editor. Only font stacks available on most devices are used,
// so nothing is loaded from outside the site.
public static class FontOptions
{
    public static IReadOnlyList<FontOption> All { get; } =
    [
        new("Georgia (serif)", "Georgia, 'Times New Roman', serif"),
        new("Georgia (plain serif)", "Georgia, serif"),
        new("Times (classic serif)", "'Times New Roman', Times, serif"),
        new("Helvetica (sans-serif)", "'Helvetica Neue', Arial, sans-serif"),
        new("Segoe UI (sans-serif)", "'Segoe UI', 'Helvetica Neue', Arial, sans-serif"),
        new("Trebuchet (rounded)", "'Trebuchet MS', 'Segoe UI', sans-serif"),
        new("Trebuchet (playful)", "'Trebuchet MS', 'Comic Sans MS', sans-serif"),
        new("Monospace", "'DejaVu Sans Mono', Consolas, monospace"),
    ];

    // The list above, plus the current value if it's not one of them.
    public static IReadOnlyList<FontOption> Including(string current) =>
        All.Any(f => f.Css == current) ? All : [.. All, new FontOption("Current font", current)];
}

// WCAG contrast ratios, used to warn about hard-to-read color combinations.
public static class ColorContrast
{
    public const double Readable = 4.5;

    public static double Ratio(string foreground, string background)
    {
        var (lighter, darker) = (Luminance(foreground), Luminance(background)) is var (a, b) && a > b ? (a, b) : (b, a);
        return (lighter + 0.05) / (darker + 0.05);
    }

    // Same as CSS color-mix(in srgb, first weight%, second).
    public static string Mix(string first, string second, double weight)
    {
        var (r1, g1, b1) = Parse(first);
        var (r2, g2, b2) = Parse(second);
        return $"#{Blend(r1, r2):x2}{Blend(g1, g2):x2}{Blend(b1, b2):x2}";

        int Blend(int x, int y) => (int)Math.Round(x * weight + y * (1 - weight));
    }

    private static double Luminance(string color)
    {
        var (r, g, b) = Parse(color);
        return 0.2126 * Channel(r) + 0.7152 * Channel(g) + 0.0722 * Channel(b);

        static double Channel(int value)
        {
            var c = value / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
    }

    private static (int R, int G, int B) Parse(string color) =>
        (Hex(color, 1), Hex(color, 3), Hex(color, 5));

    private static int Hex(string color, int start) =>
        int.Parse(color.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}

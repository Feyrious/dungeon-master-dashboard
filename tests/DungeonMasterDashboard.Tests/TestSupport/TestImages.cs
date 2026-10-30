namespace DungeonMasterDashboard.Tests.TestSupport;

// Bytes that start with the right file signatures; the API only checks those.
internal static class TestImages
{
    public static byte[] Png { get; } = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. new byte[64]];

    public static byte[] Jpeg { get; } = [0xFF, 0xD8, 0xFF, 0xE0, .. new byte[64]];

    public static byte[] WebP { get; } = [.. "RIFF"u8, 0, 0, 0, 0, .. "WEBP"u8, .. new byte[64]];
}

namespace DungeonMasterDashboard.Api.Images;

// An uploaded tile image as stored for a game system or a world.
public interface IStoredImage
{
    byte[] Data { get; set; }

    string ContentType { get; set; }

    DateTimeOffset UpdatedAt { get; set; }
}

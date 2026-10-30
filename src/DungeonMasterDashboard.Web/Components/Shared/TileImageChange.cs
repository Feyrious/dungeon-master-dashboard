namespace DungeonMasterDashboard.Web.Components.Shared;

// What should happen to a tile's image when the editor is saved.
public abstract record TileImageChange
{
    public static TileImageChange None { get; } = new Keep();

    public static TileImageChange RemoveUploaded { get; } = new Remove();

    public sealed record Keep : TileImageChange;

    public sealed record Upload(byte[] Data, string ContentType) : TileImageChange;

    public sealed record Remove : TileImageChange;
}

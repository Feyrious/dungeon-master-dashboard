namespace DungeonMasterDashboard.Tests.TestSupport;

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("dmd-tests-").FullName;

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best effort; the OS cleans up temp files eventually.
        }
    }
}

using DungeonMasterDashboard.Tests.TestSupport;

namespace DungeonMasterDashboard.Tests.Api;

// Starts a fresh API with an empty database for every test.
public abstract class ApiTestBase
{
    private TempDirectory directory = null!;

    internal ApiFactory Factory { get; private set; } = null!;

    protected HttpClient Client { get; private set; } = null!;

    protected string DatabasePath => directory.File("api.db");

    [SetUp]
    public void StartApi()
    {
        directory = new TempDirectory();
        Factory = new ApiFactory(DatabasePath, Settings);
        Client = Factory.CreateAuthorizedClient();
    }

    [TearDown]
    public void StopApi()
    {
        Client.Dispose();
        Factory.Dispose();
        directory.Dispose();
    }

    protected virtual IReadOnlyDictionary<string, string>? Settings => null;

    protected static ByteArrayContent ImageContent(byte[] data, string contentType) =>
        new(data) { Headers = { ContentType = new(contentType) } };
}

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using DungeonMasterDashboard.Api.Data;

namespace DungeonMasterDashboard.Tests.TestSupport;

// Runs the real Web API in memory with its own SQLite database.
internal sealed class ApiFactory(string databasePath, IReadOnlyDictionary<string, string>? settings = null)
    : WebApplicationFactory<DashboardDbContext>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"DataSource={databasePath}");
        builder.UseSetting("Auth:ApiToken:SigningKey", TestUsers.SigningKey);
        foreach (var (key, value) in settings ?? new Dictionary<string, string>())
        {
            builder.UseSetting(key, value);
        }
    }

    public HttpClient CreateAuthorizedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestUsers.TokenService().CreateToken(TestUsers.Signed()));
        return client;
    }
}

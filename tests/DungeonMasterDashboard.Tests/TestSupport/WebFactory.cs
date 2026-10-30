using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using DungeonMasterDashboard.Web.Data;

namespace DungeonMasterDashboard.Tests.TestSupport;

// Runs the real Blazor web app in memory with its own database and one seeded user.
// The Web API isn't running, so pages fall back to the defaults defined in code.
internal sealed partial class WebFactory(TempDirectory directory) : WebApplicationFactory<ApplicationDbContext>
{
    public const string Email = "dm@example.com";
    public const string Password = "Test-Passw0rd!";

    public string TileImagesPath => directory.File("tile-images");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(TileImagesPath);
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"DataSource={directory.File("web.db")}");
        builder.UseSetting("Auth:ApiToken:SigningKey", TestUsers.SigningKey);
        builder.UseSetting("Auth:SeedUsers:0:Email", Email);
        builder.UseSetting("Auth:SeedUsers:0:Password", Password);
        // Nothing listens here, so API calls fail fast.
        builder.UseSetting("Api:BaseUrl", "http://127.0.0.1:9");
        builder.UseSetting("TileImages:Path", TileImagesPath);
    }

    public HttpClient CreateBrowser() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    // Submits the sign-in form like a browser would.
    public static async Task<HttpResponseMessage> SignInAsync(HttpClient browser, string email = Email, string password = Password)
    {
        var page = await browser.GetStringAsync("/");
        var token = AntiforgeryToken().Match(page).Groups[1].Value;
        return await browser.PostAsync("/", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["_handler"] = "login",
            ["Input.Email"] = email,
            ["Input.Password"] = password,
        }));
    }

    public static string Location(HttpResponseMessage response) => response.Headers.Location?.OriginalString ?? "";

    public static bool IsRedirectToLogin(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.Redirect && Location(response).Contains("/Account/Login", StringComparison.Ordinal);

    [GeneratedRegex("name=\"__RequestVerificationToken\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryToken();
}

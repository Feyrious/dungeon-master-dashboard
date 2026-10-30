using System.Net;
using Bunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DungeonMasterDashboard.Tests.TestSupport;
using DungeonMasterDashboard.Web.Api;
using DungeonMasterDashboard.Web.GameSystems;

namespace DungeonMasterDashboard.Tests.Web.Components;

// bUnit context with the services the overview and editors need, a fake Web API and
// browser scripts stubbed out.
public abstract class DialogTestContext : BunitContext
{
    private Func<HttpRequestMessage, HttpResponseMessage> respond = _ => new HttpResponseMessage(HttpStatusCode.NoContent);

    internal FakeHttpHandler Api { get; }

    protected BunitJSModuleInterop Cropper { get; }

    protected DialogTestContext()
    {
        Api = new FakeHttpHandler(request => respond(request));
        JSInterop.Mode = JSRuntimeMode.Loose;
        Cropper = JSInterop.SetupModule("./js/tile-cropper.js");
        Cropper.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./js/modal.js").Mode = JSRuntimeMode.Loose;
        AddAuthorization().SetAuthorized("dm@example.com");

        var apiClient = new ApiClient(
            new HttpClient(Api) { BaseAddress = new Uri("http://api.test/") },
            new TestAuthenticationStateProvider(TestUsers.Signed()),
            TestUsers.TokenService());
        Services.AddSingleton(GameSystemCatalog.Default);
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton<IWebHostEnvironment>(new FakeWebHostEnvironment(Path.GetTempPath()));
        Services.AddSingleton<TileImages>();
        Services.AddSingleton(apiClient);
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        Services.AddSingleton<ConfiguredGameSystems>();
    }

    // How the fake Web API answers.
    internal void RespondWith(Func<HttpRequestMessage, HttpResponseMessage> response) => respond = response;

    protected void ConfirmDialogsAnswer(bool answer) => JSInterop.Setup<bool>("confirm", _ => true).SetResult(answer);

    internal IEnumerable<string> Calls => Api.Requests.Select(r => $"{r.Method} {r.Uri.AbsolutePath}");
}

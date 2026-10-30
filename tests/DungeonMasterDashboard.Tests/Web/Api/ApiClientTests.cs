using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using DungeonMasterDashboard.Contracts;
using DungeonMasterDashboard.Contracts.GameSystems;
using DungeonMasterDashboard.Contracts.Worlds;
using DungeonMasterDashboard.Tests.TestSupport;
using DungeonMasterDashboard.Web.Api;
using DungeonMasterDashboard.Web.Worlds;

namespace DungeonMasterDashboard.Tests.Web.Api;

public class ApiClientTests
{
    private static ApiClient Create(FakeHttpHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") },
        new TestAuthenticationStateProvider(TestUsers.Signed()),
        TestUsers.TokenService());

    private static WorldDto World => new() { Name = "World", Theme = WorldTiles.DefaultTheme.ToDto() };

    [Test]
    public async Task Requests_carry_a_bearer_token_for_the_current_user()
    {
        var handler = FakeHttpHandler.ReturningJson(new CurrentUserResponse("user-1", "dm@example.com", null));
        var user = await Create(handler).GetCurrentUserAsync();

        Assert.That(user?.Id, Is.EqualTo("user-1"));
        Assert.That(handler.Requests.Single().Authorization, Does.StartWith("Bearer ey"));
    }

    [Test]
    public async Task Each_call_hits_the_right_endpoint()
    {
        var handler = new FakeHttpHandler(request => request.Method == HttpMethod.Post
            ? FakeHttpHandler.Json(World with { Id = 3 }, HttpStatusCode.Created)
            : request.RequestUri!.AbsolutePath.EndsWith("/settings") && request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath != "/api/systems/settings"
                ? FakeHttpHandler.Json(new GameSystemSettingsDto { SystemId = "kult" })
                : request.Method == HttpMethod.Get
                    ? FakeHttpHandler.Json(Array.Empty<object>())
                    : new HttpResponseMessage(HttpStatusCode.NoContent));
        var api = Create(handler);

        await api.GetAllSystemSettingsAsync();
        await api.GetSystemSettingsAsync("kult");
        await api.SaveSystemSettingsAsync("kult", new GameSystemSettingsDto { Name = "K" });
        await api.ResetSystemSettingsAsync("kult");
        await api.UploadSystemImageAsync("kult", TestImages.Png, "image/png");
        await api.DeleteSystemImageAsync("kult");
        await api.GetWorldsAsync();
        var created = await api.CreateWorldAsync(World);
        await api.UpdateWorldAsync(3, World);
        await api.DeleteWorldAsync(3);
        await api.UploadWorldImageAsync(3, TestImages.WebP, "image/webp");
        await api.DeleteWorldImageAsync(3);

        Assert.That(created.Id, Is.EqualTo(3));
        Assert.That(handler.Requests.Select(r => $"{r.Method} {r.Uri.AbsolutePath}"), Is.EqualTo(new[]
        {
            "GET /api/systems/settings",
            "GET /api/systems/kult/settings",
            "PUT /api/systems/kult/settings",
            "DELETE /api/systems/kult/settings",
            "PUT /api/systems/kult/image",
            "DELETE /api/systems/kult/image",
            "GET /api/worlds",
            "POST /api/worlds",
            "PUT /api/worlds/3",
            "DELETE /api/worlds/3",
            "PUT /api/worlds/3/image",
            "DELETE /api/worlds/3/image",
        }));

        var upload = handler.Requests[4];
        Assert.That(upload.ContentType, Is.EqualTo("image/png"));
        Assert.That(upload.Body, Is.EqualTo(TestImages.Png));
        Assert.That(JsonDocument.Parse(handler.Requests[2].BodyText).RootElement.GetProperty("name").GetString(), Is.EqualTo("K"));
    }

    [Test]
    public async Task System_ids_are_escaped_in_urls()
    {
        var handler = FakeHttpHandler.Returning(HttpStatusCode.NotFound);
        await Create(handler).GetSystemSettingsAsync("a/b");
        Assert.That(handler.Requests.Single().Uri.AbsolutePath, Is.EqualTo("/api/systems/a%2Fb/settings"));
    }

    [Test]
    public async Task Missing_settings_and_images_return_null()
    {
        var api = Create(FakeHttpHandler.Returning(HttpStatusCode.NotFound));
        Assert.That(await api.GetSystemSettingsAsync("kult"), Is.Null);
        Assert.That(await api.GetSystemImageAsync("kult", TestUsers.Signed()), Is.Null);
        Assert.That(await api.GetWorldImageAsync(1, TestUsers.Signed()), Is.Null);
    }

    [Test]
    public async Task Images_are_fetched_for_the_given_user()
    {
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(TestImages.Jpeg) { Headers = { ContentType = new("image/jpeg") } },
        });

        var image = await Create(handler).GetWorldImageAsync(5, TestUsers.Signed("someone-else"));

        Assert.That(image!.Value.Data, Is.EqualTo(TestImages.Jpeg));
        Assert.That(image.Value.ContentType, Is.EqualTo("image/jpeg"));
        var token = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(handler.Requests.Single().Authorization!["Bearer ".Length..]);
        Assert.That(token.Subject, Is.EqualTo("someone-else"));
    }

    [Test]
    public void Validation_errors_become_the_exception_message()
    {
        var problem = new HttpValidationProblemDetails(new Dictionary<string, string[]> { ["Url"] = ["Use a full web address."] });
        var api = Create(new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = JsonContent.Create(problem) }));

        var error = Assert.ThrowsAsync<HttpRequestException>(() => api.CreateWorldAsync(World));
        Assert.That(error!.Message, Is.EqualTo("Use a full web address."));
        Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public void Problem_details_become_the_exception_message()
    {
        var api = Create(new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge)
        {
            Content = JsonContent.Create(new { title = "Content Too Large", detail = "Images can be at most 2 MB." }),
        }));

        var error = Assert.ThrowsAsync<HttpRequestException>(() => api.UploadWorldImageAsync(1, TestImages.Png, "image/png"));
        Assert.That(error!.Message, Is.EqualTo("Images can be at most 2 MB."));
    }

    [Test]
    public void Other_errors_report_the_status_code()
    {
        var api = Create(new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>") }));
        var error = Assert.ThrowsAsync<HttpRequestException>(() => api.GetWorldsAsync());
        Assert.That(error!.Message, Does.StartWith("The API returned 502"));
    }
}

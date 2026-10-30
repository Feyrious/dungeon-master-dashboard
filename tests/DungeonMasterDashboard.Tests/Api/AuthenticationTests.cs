using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DungeonMasterDashboard.Contracts;
using DungeonMasterDashboard.Tests.TestSupport;

namespace DungeonMasterDashboard.Tests.Api;

public class AuthenticationTests : ApiTestBase
{
    private async Task<HttpStatusCode> GetMeWithTokenAsync(string token)
    {
        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await client.GetAsync("/api/me")).StatusCode;
    }

    [Test]
    public async Task Health_check_needs_no_token()
    {
        using var client = Factory.CreateClient();
        Assert.That((await client.GetAsync("/health")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [TestCase("/api/me")]
    [TestCase("/api/systems/settings")]
    [TestCase("/api/systems/dnd5e/image")]
    [TestCase("/api/worlds")]
    [TestCase("/api/worlds/1/image")]
    public async Task Every_endpoint_requires_a_token(string path)
    {
        using var client = Factory.CreateClient();
        Assert.That((await client.GetAsync(path)).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Me_returns_the_user_from_the_token()
    {
        var me = await Client.GetFromJsonAsync<CurrentUserResponse>("/api/me");
        Assert.That(me, Is.EqualTo(new CurrentUserResponse("user-1", "dm@example.com", "dm@example.com")));
    }

    [Test]
    public async Task Garbage_tokens_are_rejected() =>
        Assert.That(await GetMeWithTokenAsync("abc.def.ghi"), Is.EqualTo(HttpStatusCode.Unauthorized));

    [Test]
    public async Task Tokens_signed_with_another_key_are_rejected()
    {
        var token = TestUsers.TokenService(signingKey: "another-signing-key-that-is-long-enough-123").CreateToken(TestUsers.Signed());
        Assert.That(await GetMeWithTokenAsync(token), Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Tokens_from_another_issuer_are_rejected()
    {
        var token = TestUsers.TokenService(issuer: "someone-else").CreateToken(TestUsers.Signed());
        Assert.That(await GetMeWithTokenAsync(token), Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Expired_tokens_are_rejected()
    {
        var token = TestUsers.TokenService(new FixedTimeProvider(DateTimeOffset.UtcNow.AddHours(-1))).CreateToken(TestUsers.Signed());
        Assert.That(await GetMeWithTokenAsync(token), Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}

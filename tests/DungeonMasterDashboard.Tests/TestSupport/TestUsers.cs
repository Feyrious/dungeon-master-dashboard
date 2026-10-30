using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using DungeonMasterDashboard.Web.Api;
using DungeonMasterDashboard.Web.Auth;

namespace DungeonMasterDashboard.Tests.TestSupport;

internal static class TestUsers
{
    public const string SigningKey = "test-signing-key-that-is-long-enough-for-hmac-sha256";
    public const string Issuer = "dungeon-master-dashboard-web";
    public const string Audience = "dungeon-master-dashboard-api";

    public static ClaimsPrincipal Signed(string id = "user-1", string name = "dm@example.com", string? email = "dm@example.com")
    {
        List<Claim> claims = [new(ClaimTypes.NameIdentifier, id), new(ClaimTypes.Name, name)];
        if (email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    public static ApiTokenService TokenService(TimeProvider? time = null, string signingKey = SigningKey, string issuer = Issuer, int lifetimeMinutes = 5) =>
        new(Options.Create(new AuthOptions
        {
            ApiToken = new ApiTokenOptions { SigningKey = signingKey, Issuer = issuer, Audience = Audience, LifetimeMinutes = lifetimeMinutes },
        }), time ?? TimeProvider.System);
}

internal sealed class TestAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

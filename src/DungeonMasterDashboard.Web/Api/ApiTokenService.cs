using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using DungeonMasterDashboard.Web.Auth;

namespace DungeonMasterDashboard.Web.Api;

// Issues short-lived tokens that carry the signed-in user's identity to the Web API.
public sealed class ApiTokenService(IOptions<AuthOptions> options, TimeProvider timeProvider)
{
    private readonly JsonWebTokenHandler handler = new();

    public string CreateToken(ClaimsPrincipal user)
    {
        var settings = options.Value.ApiToken;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("User has no identifier."),
            [JwtRegisteredClaimNames.Name] = user.Identity?.Name ?? "",
        };
        if (user.FindFirstValue(ClaimTypes.Email) is { } email)
        {
            claims[JwtRegisteredClaimNames.Email] = email;
        }

        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Claims = claims,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(settings.LifetimeMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        });
    }
}

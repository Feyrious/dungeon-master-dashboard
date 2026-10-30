using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using DungeonMasterDashboard.Tests.TestSupport;

namespace DungeonMasterDashboard.Tests.Web.Api;

public class ApiTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private static async Task<TokenValidationResult> ValidateAsync(string token, string key = TestUsers.SigningKey) =>
        await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = TestUsers.Issuer,
            ValidAudience = TestUsers.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = false,
        });

    [Test]
    public async Task Token_carries_the_users_id_name_and_email()
    {
        var token = TestUsers.TokenService().CreateToken(TestUsers.Signed("abc", "Kim", "kim@example.com"));
        var result = await ValidateAsync(token);

        Assert.That(result.IsValid, Is.True, result.Exception?.Message);
        var jwt = (JsonWebToken)result.SecurityToken;
        Assert.Multiple(() =>
        {
            Assert.That(jwt.Subject, Is.EqualTo("abc"));
            Assert.That(jwt.GetClaim(JwtRegisteredClaimNames.Name).Value, Is.EqualTo("Kim"));
            Assert.That(jwt.GetClaim(JwtRegisteredClaimNames.Email).Value, Is.EqualTo("kim@example.com"));
            Assert.That(jwt.Alg, Is.EqualTo(SecurityAlgorithms.HmacSha256));
        });
    }

    [Test]
    public void Token_lives_for_the_configured_lifetime()
    {
        var token = new JsonWebToken(TestUsers.TokenService(new FixedTimeProvider(Now), lifetimeMinutes: 5).CreateToken(TestUsers.Signed()));
        Assert.That(token.ValidTo, Is.EqualTo(Now.UtcDateTime.AddMinutes(5)));
        Assert.That(token.IssuedAt, Is.EqualTo(Now.UtcDateTime));
    }

    [Test]
    public void Email_is_left_out_when_the_user_has_none()
    {
        var token = new JsonWebToken(TestUsers.TokenService().CreateToken(TestUsers.Signed(email: null)));
        Assert.That(token.TryGetClaim(JwtRegisteredClaimNames.Email, out _), Is.False);
    }

    [Test]
    public async Task Token_is_rejected_with_another_key()
    {
        var token = TestUsers.TokenService().CreateToken(TestUsers.Signed());
        var result = await ValidateAsync(token, "some-other-signing-key-of-sufficient-length!!");
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Users_without_an_id_get_no_token() =>
        Assert.Throws<InvalidOperationException>(() => TestUsers.TokenService().CreateToken(new ClaimsPrincipal(new ClaimsIdentity())));
}

using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Requests come from the Blazor frontend with a short-lived token signed with a shared key.
var apiToken = builder.Configuration.GetSection("Auth:ApiToken");
var signingKey = apiToken["SigningKey"];
if (signingKey is not { Length: >= 32 })
{
    throw new InvalidOperationException("Auth:ApiToken:SigningKey must be at least 32 characters.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = apiToken["Issuer"],
            ValidAudience = apiToken["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            NameClaimType = JwtRegisteredClaimNames.Name,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

// Every endpoint requires an authenticated user unless it opts out with AllowAnonymous.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok()).AllowAnonymous();

app.MapGet("/api/me", (ClaimsPrincipal user) => new CurrentUserResponse(
    user.FindFirstValue(JwtRegisteredClaimNames.Sub)!,
    user.Identity?.Name ?? "",
    user.FindFirstValue(JwtRegisteredClaimNames.Email)))
.WithName("GetCurrentUser");

app.Run();

record CurrentUserResponse(string Id, string Name, string? Email);

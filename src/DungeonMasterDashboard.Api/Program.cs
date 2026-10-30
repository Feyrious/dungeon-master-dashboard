using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using DungeonMasterDashboard.Api.Data;
using DungeonMasterDashboard.Api.GameSystems;
using DungeonMasterDashboard.Api.Worlds;
using DungeonMasterDashboard.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// SQLite resolves relative paths against the working directory; anchor them to the app instead.
var connection = new SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
connection.DataSource = Path.GetFullPath(connection.DataSource, builder.Environment.ContentRootPath);
Directory.CreateDirectory(Path.GetDirectoryName(connection.DataSource)!);
builder.Services.AddDbContext<DashboardDbContext>(options => options.UseSqlite(connection.ConnectionString));
builder.Services.AddSingleton(TimeProvider.System);

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
    db.Database.Migrate();
    await WorldLinkImport.ImportAsync(db, app.Configuration, app.Logger);
}

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

app.MapGameSystemSettings();
app.MapWorlds();

app.Run();

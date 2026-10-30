namespace DungeonMasterDashboard.Web.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public ApiTokenOptions ApiToken { get; set; } = new();

    // Accounts created on startup if they don't exist yet. There is no public sign-up.
    public List<SeedUserOptions> SeedUsers { get; set; } = [];
}

public sealed class ApiTokenOptions
{
    public string SigningKey { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int LifetimeMinutes { get; set; } = 5;
}

public sealed class SeedUserOptions
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

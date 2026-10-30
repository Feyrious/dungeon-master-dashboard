using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using DungeonMasterDashboard.Web.Data;

namespace DungeonMasterDashboard.Web.Auth;

// Creates the configured accounts on startup. Existing accounts are never modified,
// so a password changed in the app is not overwritten on the next restart.
internal static class UserSeeder
{
    public static async Task SeedUsersAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AuthOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(UserSeeder));

        foreach (var seed in options.SeedUsers.Where(u => !string.IsNullOrWhiteSpace(u.Email)))
        {
            if (await userManager.FindByEmailAsync(seed.Email) is not null)
            {
                continue;
            }

            var user = new ApplicationUser { UserName = seed.Email, Email = seed.Email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, seed.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Could not create seed user '{seed.Email}': {errors}");
            }

            logger.LogInformation("Created user {Email}.", seed.Email);
        }

        if (!userManager.Users.Any())
        {
            logger.LogWarning("No users exist. Set Auth__SeedUsers__0__Email and Auth__SeedUsers__0__Password to create one.");
        }
    }
}

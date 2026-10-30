using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DungeonMasterDashboard.Web.Auth;
using DungeonMasterDashboard.Web.Data;

namespace DungeonMasterDashboard.Tests.Web.Auth;

public class UserSeederTests
{
    private SqliteConnection connection = null!;

    [SetUp]
    public void SetUp()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
    }

    [TearDown]
    public void TearDown() => connection.Dispose();

    private ServiceProvider CreateServices(params SeedUserOptions[] users)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        // Same schema version as the app: EF caches the model per context type, so a different
        // Identity setup here would leak into the web host tests in the same test run.
        services.AddIdentityCore<ApplicationUser>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.Configure<AuthOptions>(options => options.SeedUsers = [.. users]);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
        return provider;
    }

    private static async Task<List<ApplicationUser>> UsersAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().Users.ToListAsync();
    }

    [Test]
    public async Task Creates_configured_users_with_confirmed_email()
    {
        using var provider = CreateServices(new SeedUserOptions { Email = "dm@example.com", Password = "Test-Passw0rd!" });

        await provider.SeedUsersAsync();

        var user = (await UsersAsync(provider)).Single();
        Assert.Multiple(() =>
        {
            Assert.That(user.Email, Is.EqualTo("dm@example.com"));
            Assert.That(user.UserName, Is.EqualTo("dm@example.com"));
            Assert.That(user.EmailConfirmed, Is.True);
        });
    }

    [Test]
    public async Task Never_changes_an_existing_users_password()
    {
        using var first = CreateServices(new SeedUserOptions { Email = "dm@example.com", Password = "Test-Passw0rd!" });
        await first.SeedUsersAsync();

        using var second = CreateServices(new SeedUserOptions { Email = "dm@example.com", Password = "Other-Passw0rd!" });
        await second.SeedUsersAsync();

        using var scope = second.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("dm@example.com");
        Assert.That(await userManager.CheckPasswordAsync(user!, "Test-Passw0rd!"), Is.True);
    }

    [Test]
    public async Task Skips_entries_without_email_or_password()
    {
        using var provider = CreateServices(
            new SeedUserOptions { Email = "", Password = "Test-Passw0rd!" },
            new SeedUserOptions { Email = "nopass@example.com", Password = "" },
            new SeedUserOptions { Email = "ok@example.com", Password = "Test-Passw0rd!" });

        await provider.SeedUsersAsync();

        Assert.That((await UsersAsync(provider)).Select(u => u.Email), Is.EqualTo(new[] { "ok@example.com" }));
    }

    [Test]
    public void Fails_loudly_for_a_weak_password()
    {
        using var provider = CreateServices(new SeedUserOptions { Email = "dm@example.com", Password = "weak" });
        var error = Assert.ThrowsAsync<InvalidOperationException>(() => provider.SeedUsersAsync());
        Assert.That(error!.Message, Does.Contain("Could not create seed user 'dm@example.com'"));
    }
}

using System.Net;
using DungeonMasterDashboard.Tests.TestSupport;

namespace DungeonMasterDashboard.Tests.Web;

// Runs the real web app in memory. The API isn't running, so pages show the defaults from code.
public class WebHostTests
{
    private TempDirectory? directory;
    private WebFactory? factory;
    private HttpClient? browser;

    [SetUp]
    public void SetUp()
    {
        browser = null;
        directory = new TempDirectory();
        factory = new WebFactory(directory);
        browser = factory.CreateBrowser();
    }

    [TearDown]
    public void TearDown()
    {
        browser?.Dispose();
        factory?.Dispose();
        directory?.Dispose();
    }

    [Test]
    public async Task The_landing_page_is_the_sign_in_page()
    {
        var response = await browser!.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(html, Does.Contain("<title>Sign in · Dungeon Master Dashboard</title>").And.Contain("Sign in with a passkey"));
        Assert.That(html, Does.Not.Contain("Register"));
    }

    [TestCase("/systems")]
    [TestCase("/systems/dnd5e")]
    [TestCase("/systems/kult/dice")]
    [TestCase("/Account/Manage")]
    [TestCase("/tile-images/dnd5e")]
    [TestCase("/system-images/dnd5e")]
    [TestCase("/world-images/1")]
    public async Task Pages_and_images_need_a_signed_in_user(string path) =>
        Assert.That(WebFactory.IsRedirectToLogin(await browser!.GetAsync(path)), Is.True);

    [TestCase("/Account/Register")]
    [TestCase("/Account/ForgotPassword")]
    [TestCase("/Account/ExternalLogin")]
    public async Task Sign_up_and_email_pages_do_not_exist(string path) =>
        Assert.That((await browser!.GetAsync(path)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

    [Test]
    public async Task Signing_in_leads_to_the_overview()
    {
        var response = await WebFactory.SignInAsync(browser!);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(WebFactory.Location(response), Does.EndWith("/systems"));

        var overview = await browser!.GetStringAsync("/systems");
        Assert.That(overview, Does.Contain("Dungeons &amp; Dragons 5e").And.Contain("Tails of Equestria").And.Contain("Add world"));
    }

    [Test]
    public async Task Signed_in_visitors_skip_the_sign_in_page()
    {
        await WebFactory.SignInAsync(browser!);
        var response = await browser!.GetAsync("/");
        Assert.That(WebFactory.Location(response), Does.EndWith("systems"));
    }

    [Test]
    public async Task Wrong_password_and_unknown_user_get_the_same_message()
    {
        var wrongPassword = await (await WebFactory.SignInAsync(browser!, password: "Wrong-Passw0rd!")).Content.ReadAsStringAsync();
        var unknownUser = await (await WebFactory.SignInAsync(browser!, email: "nobody@example.com")).Content.ReadAsStringAsync();

        Assert.That(wrongPassword, Does.Contain("Invalid login attempt"));
        Assert.That(unknownUser, Does.Contain("Invalid login attempt"));
    }

    [Test]
    public async Task Five_wrong_passwords_lock_the_account()
    {
        HttpResponseMessage response = null!;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            response = await WebFactory.SignInAsync(browser!, password: "Wrong-Passw0rd!");
        }

        Assert.That(WebFactory.Location(response), Does.EndWith("/Account/Lockout"));
        var correct = await WebFactory.SignInAsync(browser!);
        Assert.That(WebFactory.Location(correct), Does.EndWith("/Account/Lockout"), "locked even with the right password");
    }

    [Test]
    public async Task Tool_pages_follow_the_catalog()
    {
        await WebFactory.SignInAsync(browser!);

        Assert.That((await browser!.GetAsync("/systems/dnd5e/encounters")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await browser!.GetAsync("/systems/kult/monsters")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await browser!.GetAsync("/systems/unknown")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var kultDice = await browser!.GetStringAsync("/systems/kult/dice");
        Assert.That(kultDice, Does.Contain("Roll any dice expression used in KULT: Divinity Lost"));
    }

    [Test]
    public async Task Custom_tile_images_are_served_to_signed_in_users()
    {
        File.WriteAllBytes(Path.Combine(factory!.TileImagesPath, "kult.png"), TestImages.Png);
        await WebFactory.SignInAsync(browser!);

        var image = await browser!.GetAsync("/tile-images/kult");
        Assert.That(image.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(image.Content.Headers.ContentType?.MediaType, Is.EqualTo("image/png"));
        Assert.That((await browser!.GetAsync("/tile-images/dnd5e")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var overview = await browser!.GetStringAsync("/systems");
        Assert.That(overview, Does.Contain("tile-images/kult?v="));
    }

    [Test]
    public async Task Uploaded_images_are_not_found_while_the_api_is_away()
    {
        await WebFactory.SignInAsync(browser!);
        Assert.That((await browser!.GetAsync("/system-images/unknown-system")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [TestCase("/Account/Manage")]
    [TestCase("/Account/Manage/ChangePassword")]
    [TestCase("/Account/Manage/TwoFactorAuthentication")]
    [TestCase("/Account/Manage/EnableAuthenticator")]
    [TestCase("/Account/Manage/ResetAuthenticator")]
    [TestCase("/Account/Manage/Passkeys")]
    public async Task Account_pages_work_for_signed_in_users(string path)
    {
        await WebFactory.SignInAsync(browser!);
        var response = await browser!.GetAsync(path);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("Account settings").And.Contain("← Overview"));
    }

    // These used to fail with a server error when two-factor wasn't on.
    [TestCase("/Account/Manage/Disable2fa")]
    [TestCase("/Account/Manage/GenerateRecoveryCodes")]
    public async Task Two_factor_pages_send_you_back_when_two_factor_is_off(string path)
    {
        await WebFactory.SignInAsync(browser!);
        var response = await browser!.GetAsync(path);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(WebFactory.Location(response), Does.EndWith("/Account/Manage/TwoFactorAuthentication"));
        Assert.That(await browser.GetStringAsync(WebFactory.Location(response)), Does.Contain("Two-factor authentication isn&#x27;t enabled."));
    }

    [Test]
    public async Task The_password_can_be_changed()
    {
        await WebFactory.SignInAsync(browser!);
        var response = await WebFactory.PostFormAsync(browser!, "/Account/Manage/ChangePassword", new Dictionary<string, string>
        {
            ["_handler"] = "change-password",
            ["Input.OldPassword"] = WebFactory.Password,
            ["Input.NewPassword"] = "Brand-New-Passw0rd!",
            ["Input.ConfirmPassword"] = "Brand-New-Passw0rd!",
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));

        using var other = factory!.CreateBrowser();
        Assert.That(await (await WebFactory.SignInAsync(other)).Content.ReadAsStringAsync(), Does.Contain("Invalid login attempt"));
        Assert.That(WebFactory.Location(await WebFactory.SignInAsync(other, password: "Brand-New-Passw0rd!")), Does.EndWith("/systems"));
    }

    [Test]
    public async Task The_account_menu_offers_settings_and_log_out()
    {
        await WebFactory.SignInAsync(browser!);
        var overview = await browser!.GetStringAsync("/systems");
        Assert.That(overview, Does.Contain("href=\"Account/Manage\"").And.Contain("action=\"Account/Logout\""));
    }

    [Test]
    public async Task Logging_out_returns_to_the_sign_in_page()
    {
        await WebFactory.SignInAsync(browser!);
        var response = await WebFactory.PostFormAsync(browser!, "/Account/Logout", [], formPage: "/Account/Manage");

        Assert.That(WebFactory.Location(response), Is.EqualTo("/"));
        Assert.That(WebFactory.IsRedirectToLogin(await browser!.GetAsync("/systems")), Is.True);
    }
}

using DungeonMasterDashboard.Contracts;

namespace DungeonMasterDashboard.Tests.Contracts;

public class SettingsLimitsTests
{
    [TestCase("https://example.com")]
    [TestCase("http://example.com/wiki/page?x=1#top")]
    [TestCase("https://my-wiki.example.org:8443/journals")]
    public void IsWebAddress_accepts_http_and_https(string value) =>
        Assert.That(SettingsLimits.IsWebAddress(value), Is.True);

    [TestCase("javascript:alert(1)")]
    [TestCase("data:text/html,<b>hi</b>")]
    [TestCase("ftp://example.com/file")]
    [TestCase("file:///etc/passwd")]
    [TestCase("/relative/path")]
    [TestCase("example.com")]
    [TestCase("")]
    public void IsWebAddress_rejects_other_schemes_and_relative_addresses(string value) =>
        Assert.That(SettingsLimits.IsWebAddress(value), Is.False);

    [Test]
    public void IsWebAddress_rejects_addresses_over_the_length_limit()
    {
        var url = "https://example.com/" + new string('a', SettingsLimits.UrlMaxLength);
        Assert.That(SettingsLimits.IsWebAddress(url), Is.False);
    }

    [TestCase("#000000", true)]
    [TestCase("#A1b2C3", true)]
    [TestCase("#fff", false)]
    [TestCase("red", false)]
    [TestCase("#12345g", false)]
    [TestCase("#123456;background:red", false)]
    public void Color_only_matches_six_digit_hex(string value, bool expected) =>
        Assert.That(SettingsLimits.Color().IsMatch(value), Is.EqualTo(expected));

    [TestCase("Georgia, 'Times New Roman', serif", true)]
    [TestCase("\"Segoe UI\", sans-serif", true)]
    [TestCase("Arial;background:url(http://evil)", false)]
    [TestCase("Arial} body{display:none", false)]
    [TestCase("Arial<script>", false)]
    [TestCase("", false)]
    public void FontFamily_rejects_characters_that_escape_the_declaration(string value, bool expected) =>
        Assert.That(SettingsLimits.FontFamily().IsMatch(value), Is.EqualTo(expected));

    [TestCase("dnd5e", true)]
    [TestCase("tails-of-equestria", true)]
    [TestCase("Upper", false)]
    [TestCase("../etc", false)]
    [TestCase("with space", false)]
    [TestCase("", false)]
    public void Id_only_matches_lowercase_slugs(string value, bool expected) =>
        Assert.That(SettingsLimits.Id().IsMatch(value), Is.EqualTo(expected));

    [Test]
    public void Id_rejects_ids_over_64_characters() =>
        Assert.That(SettingsLimits.Id().IsMatch(new string('a', 65)), Is.False);

    [Test]
    public void Image_content_types_are_png_jpeg_and_webp() =>
        Assert.That(SettingsLimits.ImageContentTypes, Is.EquivalentTo(new[] { "image/png", "image/jpeg", "image/webp" }));
}

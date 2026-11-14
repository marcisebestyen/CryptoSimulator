using CryptoSimulator.Configuration;
using Microsoft.Extensions.Configuration;

namespace CryptoSimulator.Tests.Unit;

public class JwtSettingsTests
{
    private static readonly string GoodKey = new('k', JwtSettings.MinKeyLength);

    private static IConfiguration Config(params (string Key, string Value)[] items) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(items.Select(i => new KeyValuePair<string, string?>(i.Key, i.Value)))
            .Build();

    private static Dictionary<string, string> Env(params (string Key, string Value)[] items) =>
        items.ToDictionary(i => i.Key, i => i.Value);

    [Fact]
    public void Should_LoadFromConfiguration_When_AllValuesSet()
    {
        var s = JwtSettings.Load(
            Config(("Jwt:Key", GoodKey), ("Jwt:Issuer", "i"), ("Jwt:Audience", "a"), ("Jwt:ExpiryMinutes", "15")),
            Env());

        Assert.Equal(GoodKey, s.Key);
        Assert.Equal("i", s.Issuer);
        Assert.Equal("a", s.Audience);
        Assert.Equal(15, s.ExpiryMinutes);
    }

    [Fact]
    public void Should_LoadFromEnv_When_ConfigurationEmpty()
    {
        var s = JwtSettings.Load(Config(),
            Env(("JWT_KEY", GoodKey), ("JWT_ISSUER", "ei"), ("JWT_AUDIENCE", "ea"), ("JWT_EXPIRY_MINUTES", "30")));

        Assert.Equal(GoodKey, s.Key);
        Assert.Equal("ei", s.Issuer);
        Assert.Equal("ea", s.Audience);
        Assert.Equal(30, s.ExpiryMinutes);
    }

    [Fact]
    public void Should_PreferConfiguration_When_EnvAlsoSet()
    {
        var s = JwtSettings.Load(
            Config(("Jwt:Key", GoodKey), ("Jwt:Issuer", "ci"), ("Jwt:Audience", "ca"), ("Jwt:ExpiryMinutes", "5")),
            Env(("JWT_KEY", new string('z', 40)), ("JWT_ISSUER", "ei"), ("JWT_AUDIENCE", "ea"), ("JWT_EXPIRY_MINUTES", "99")));

        Assert.Equal(GoodKey, s.Key);
        Assert.Equal("ci", s.Issuer);
        Assert.Equal("ca", s.Audience);
        Assert.Equal(5, s.ExpiryMinutes);
    }

    [Fact]
    public void Should_DefaultExpiryTo60_When_NotConfigured()
    {
        var s = JwtSettings.Load(Config(("Jwt:Key", GoodKey), ("Jwt:Issuer", "i"), ("Jwt:Audience", "a")), Env());

        Assert.Equal(60, s.ExpiryMinutes);
    }

    [Fact]
    public void Should_Throw_When_KeyMissing()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            JwtSettings.Load(Config(("Jwt:Issuer", "i"), ("Jwt:Audience", "a")), Env()));

        Assert.Contains("JWT_KEY", ex.Message);
    }

    [Fact]
    public void Should_Throw_When_KeyTooShort()
    {
        Assert.Throws<InvalidOperationException>(() =>
            JwtSettings.Load(Config(("Jwt:Key", new string('k', JwtSettings.MinKeyLength - 1)), ("Jwt:Issuer", "i"), ("Jwt:Audience", "a")), Env()));
    }

    [Fact]
    public void Should_Throw_When_IssuerMissing()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            JwtSettings.Load(Config(("Jwt:Key", GoodKey), ("Jwt:Audience", "a")), Env()));

        Assert.Contains("JWT_ISSUER", ex.Message);
    }

    [Fact]
    public void Should_Throw_When_AudienceMissing()
    {
        Assert.Throws<InvalidOperationException>(() =>
            JwtSettings.Load(Config(("Jwt:Key", GoodKey), ("Jwt:Issuer", "i")), Env()));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public void Should_Throw_When_ExpiryNotPositive(string minutes)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            JwtSettings.Load(Config(("Jwt:Key", GoodKey), ("Jwt:Issuer", "i"), ("Jwt:Audience", "a"), ("Jwt:ExpiryMinutes", minutes)), Env()));

        Assert.Contains("expiry", ex.Message);
    }

    [Fact]
    public void Should_Throw_When_EnvExpiryNotNumeric()
    {
        Assert.Throws<FormatException>(() =>
            JwtSettings.Load(Config(("Jwt:Key", GoodKey), ("Jwt:Issuer", "i"), ("Jwt:Audience", "a")),
                Env(("JWT_EXPIRY_MINUTES", "abc"))));
    }
}

using CryptoSimulator.Configuration;
using Microsoft.Extensions.Configuration;

namespace CryptoSimulator.Tests.Unit;

public class AdminSettingsTests
{
    private static IConfiguration Config(params (string Key, string Value)[] items) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(items.Select(i => new KeyValuePair<string, string?>(i.Key, i.Value)))
            .Build();

    private static Dictionary<string, string> Env(params (string Key, string Value)[] items) =>
        items.ToDictionary(i => i.Key, i => i.Value);

    private static Dictionary<string, string> FullEnv() =>
        Env(("ADMIN_USERNAME", "admin"), ("ADMIN_EMAIL", "admin@example.com"), ("ADMIN_PASSWORD", "long-enough-password"));

    [Fact]
    public void Should_LoadFromEnv_When_ConfigurationEmpty()
    {
        var s = AdminSettings.Load(Config(), FullEnv());

        Assert.Equal("admin", s.Username);
        Assert.Equal("admin@example.com", s.Email);
        Assert.Equal("long-enough-password", s.Password);
    }

    [Fact]
    public void Should_PreferConfiguration_When_BothSet()
    {
        var s = AdminSettings.Load(
            Config(("Admin:Username", "root"), ("Admin:Email", "root@example.com"), ("Admin:Password", "another-long-password")),
            FullEnv());

        Assert.Equal("root", s.Username);
        Assert.Equal("root@example.com", s.Email);
        Assert.Equal("another-long-password", s.Password);
    }

    [Theory]
    [InlineData("ADMIN_USERNAME", "Admin__Username")]
    [InlineData("ADMIN_EMAIL", "Admin__Email")]
    [InlineData("ADMIN_PASSWORD", "Admin__Password")]
    public void Should_Throw_When_VariableMissing(string missing, string configName)
    {
        var env = FullEnv();
        env.Remove(missing);

        var ex = Assert.Throws<InvalidOperationException>(() => AdminSettings.Load(Config(), env));

        Assert.Contains(missing, ex.Message);
        Assert.Contains(configName, ex.Message);
    }

    [Fact]
    public void Should_Throw_When_PasswordTooShort()
    {
        var env = FullEnv();
        env["ADMIN_PASSWORD"] = new string('p', AdminSettings.MinPasswordLength - 1);

        var ex = Assert.Throws<InvalidOperationException>(() => AdminSettings.Load(Config(), env));

        Assert.Contains("ADMIN_PASSWORD", ex.Message);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a b@example.com")]
    public void Should_Throw_When_EmailInvalid(string email)
    {
        var env = FullEnv();
        env["ADMIN_EMAIL"] = email;

        var ex = Assert.Throws<InvalidOperationException>(() => AdminSettings.Load(Config(), env));

        Assert.Contains("ADMIN_EMAIL", ex.Message);
    }
}

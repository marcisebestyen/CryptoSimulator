using CryptoSimulator.Configuration;
using Microsoft.Extensions.Configuration;

namespace CryptoSimulator.Tests.Unit;

public class RecoveryCodeSettingsTests
{
    private static readonly string GoodKey = new('k', 32);

    private static IConfiguration Config(params (string Key, string Value)[] items) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(items.Select(i => new KeyValuePair<string, string?>(i.Key, i.Value)))
            .Build();

    private static Dictionary<string, string> Env(params (string Key, string Value)[] items) =>
        items.ToDictionary(i => i.Key, i => i.Value);

    [Fact]
    public void Should_LoadFromConfiguration_When_KeySet()
    {
        var s = RecoveryCodeSettings.Load(Config(("RecoveryCode:Key", GoodKey)), Env());

        Assert.Equal(GoodKey, s.Key);
    }

    [Fact]
    public void Should_LoadFromEnv_When_ConfigurationEmpty()
    {
        var s = RecoveryCodeSettings.Load(Config(), Env(("RECOVERY_CODE_KEY", GoodKey)));

        Assert.Equal(GoodKey, s.Key);
    }

    [Fact]
    public void Should_PreferConfiguration_When_BothSet()
    {
        var configKey = new string('c', 32);

        var s = RecoveryCodeSettings.Load(Config(("RecoveryCode:Key", configKey)), Env(("RECOVERY_CODE_KEY", GoodKey)));

        Assert.Equal(configKey, s.Key);
    }

    [Fact]
    public void Should_Throw_When_KeyMissing()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => RecoveryCodeSettings.Load(Config(), Env()));

        Assert.Contains("RECOVERY_CODE_KEY", ex.Message);
        Assert.Contains("RecoveryCode__Key", ex.Message);
    }

    [Fact]
    public void Should_Throw_When_KeyShorterThan32()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RecoveryCodeSettings.Load(Config(("RecoveryCode:Key", new string('k', 31))), Env()));
    }
}

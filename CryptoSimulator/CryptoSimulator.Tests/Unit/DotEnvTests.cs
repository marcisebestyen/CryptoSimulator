using CryptoSimulator.Configuration;

namespace CryptoSimulator.Tests.Unit;

public sealed class DotEnvTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dotenv-" + Guid.NewGuid().ToString("N"));

    public DotEnvTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Dir(string name)
    {
        var path = Path.Combine(_root, name);
        Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public void Should_ParseKeyValuePairs_When_EnvFileInStartDirectory()
    {
        File.WriteAllText(Path.Combine(_root, ".env"), "A=1\nB = two words \n");

        var values = DotEnv.Load(_root);

        Assert.Equal("1", values["A"]);
        Assert.Equal("two words", values["B"]);
    }

    [Fact]
    public void Should_SkipCommentsBlankAndMalformedLines_When_Parsing()
    {
        File.WriteAllText(Path.Combine(_root, ".env"), "# c=1\n\nnoequals\n=novalue\nOK=yes\n");

        var values = DotEnv.Load(_root);

        Assert.Equal(new[] { "OK" }, values.Keys);
    }

    [Fact]
    public void Should_KeepEqualsInValue_When_ValueContainsEquals()
    {
        File.WriteAllText(Path.Combine(_root, ".env"), "CONN=a=b;c=d\n");

        Assert.Equal("a=b;c=d", DotEnv.Load(_root)["CONN"]);
    }

    [Fact]
    public void Should_LetLastValueWin_When_KeyRepeated()
    {
        File.WriteAllText(Path.Combine(_root, ".env"), "A=1\nA=2\n");

        Assert.Equal("2", DotEnv.Load(_root)["A"]);
    }

    [Fact]
    public void Should_FindEnvInParent_When_StartDirectoryHasNone()
    {
        File.WriteAllText(Path.Combine(_root, ".env"), "P=parent\n");
        var child = Dir("a/b");

        Assert.Equal("parent", DotEnv.Load(child)["P"]);
    }

    [Fact]
    public void Should_UseNearestFileOnly_When_MultipleEnvFilesExist()
    {
        File.WriteAllText(Path.Combine(_root, ".env"), "P=parent\nONLY_PARENT=1\n");
        var child = Dir("child");
        File.WriteAllText(Path.Combine(child, ".env"), "P=child\n");

        var values = DotEnv.Load(child);

        Assert.Equal("child", values["P"]);
        Assert.False(values.ContainsKey("ONLY_PARENT"));
    }

    [Fact]
    public void Should_ReturnEmpty_When_EmptyEnvFile()
    {
        File.WriteAllText(Path.Combine(_root, ".env"), "");

        Assert.Empty(DotEnv.Load(_root));
    }
}

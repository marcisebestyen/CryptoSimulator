using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CryptoSimulator.Configuration;
using CryptoSimulator.Services;

namespace CryptoSimulator.Tests.Unit;

public class RecoveryCodeServiceTests
{
    private const int Samples = 1000;

    private static RecoveryCodeService Create(char keyChar = 'k') =>
        new(new RecoveryCodeSettings { Key = new string(keyChar, 32) });

    private static List<string> GenerateMany(RecoveryCodeService service) =>
        Enumerable.Range(0, Samples).Select(_ => service.Generate()).ToList();

    [Fact]
    public void Should_Generate8Chars_When_Called()
    {
        Assert.Equal(8, Create().Generate().Length);
    }

    [Fact]
    public void Should_UseOnlyAllowedChars()
    {
        var allowed = new Regex(@"^[A-Za-z0-9!@#$%^&*?\-_+=]{8}$");

        Assert.All(GenerateMany(Create()), code => Assert.Matches(allowed, code));
    }

    [Fact]
    public void Should_ContainLetterDigitAndSymbol()
    {
        Assert.All(GenerateMany(Create()), code =>
        {
            Assert.Contains(code, char.IsAsciiLetter);
            Assert.Contains(code, char.IsAsciiDigit);
            Assert.Contains(code, c => !char.IsAsciiLetterOrDigit(c));
        });
    }

    [Fact]
    public void Should_ProduceDifferentCodes()
    {
        Assert.True(GenerateMany(Create()).Distinct().Count() > 990);
    }

    [Fact]
    public void Should_HashDeterministically_When_SameCodeAndKey()
    {
        Assert.Equal(Create().Hash("abc12345"), Create().Hash("abc12345"));
    }

    [Fact]
    public void Should_MatchHmacSha256Base64_When_Hashing()
    {
        var key = new string('k', 32);
        var expected = Convert.ToBase64String(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes("abc12345")));

        Assert.Equal(expected, Create().Hash("abc12345"));
    }

    [Fact]
    public void Should_HashDifferently_When_KeyDiffers()
    {
        Assert.NotEqual(Create('a').Hash("abc12345"), Create('b').Hash("abc12345"));
    }

    [Fact]
    public void Should_HashDifferently_When_CodeDiffers()
    {
        Assert.NotEqual(Create().Hash("abc12345"), Create().Hash("abc12346"));
    }
}

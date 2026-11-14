using System.Security.Claims;
using System.Text;
using CryptoSimulator.Configuration;
using CryptoSimulator.Entities;
using CryptoSimulator.Services;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CryptoSimulator.Tests.Unit;

public class TokenServiceExtraTests
{
    private static JwtSettings Settings(int minutes = 60, string key = "unit-tests-signing-key-0123456789ab") => new()
    {
        Key = key,
        Issuer = "issuer",
        Audience = "audience",
        ExpiryMinutes = minutes,
    };

    private static JsonWebToken Read(string token) => new JsonWebTokenHandler().ReadJsonWebToken(token);

    [Fact]
    public void Should_ContainIdAndEmailClaims_When_TokenCreated()
    {
        var token = Read(new TokenService(Settings()).CreateToken(new User { Id = 7, Email = "a@b.com" }));

        Assert.Equal("7", token.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("a@b.com", token.Claims.First(c => c.Type == ClaimTypes.Email).Value);
    }

    [Fact]
    public void Should_SetIssuerAndAudience_When_TokenCreated()
    {
        var token = Read(new TokenService(Settings()).CreateToken(new User { Id = 1, Email = "a@b.com" }));

        Assert.Equal("issuer", token.Issuer);
        Assert.Contains("audience", token.Audiences);
    }

    [Fact]
    public void Should_ExpireAfterConfiguredMinutes_When_TokenCreated()
    {
        var before = DateTime.UtcNow;

        var token = Read(new TokenService(Settings(minutes: 15)).CreateToken(new User { Id = 1, Email = "a@b.com" }));

        Assert.InRange(token.ValidTo, before.AddMinutes(15).AddSeconds(-2), DateTime.UtcNow.AddMinutes(15).AddSeconds(2));
    }

    [Fact]
    public async Task Should_FailValidation_When_SignedWithDifferentKey()
    {
        var token = new TokenService(Settings()).CreateToken(new User { Id = 1, Email = "a@b.com" });

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "issuer",
            ValidAudience = "audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("another-signing-key-0123456789abcd")),
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Throw_When_UserEmailNull()
    {
        var sut = new TokenService(Settings());

        Assert.ThrowsAny<Exception>(() => sut.CreateToken(new User { Id = 1, Email = null! }));
    }
}

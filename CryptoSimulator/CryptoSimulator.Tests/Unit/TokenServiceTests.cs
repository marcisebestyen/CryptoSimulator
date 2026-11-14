using System.Text;
using CryptoSimulator.Configuration;
using CryptoSimulator.Entities;
using CryptoSimulator.Services;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CryptoSimulator.Tests.Unit;

public class TokenServiceTests
{
    private static readonly JwtSettings Settings = new()
    {
        Key = "unit-tests-signing-key-0123456789ab",
        Issuer = "issuer",
        Audience = "audience",
    };

    [Fact]
    public async Task CreateToken_ReturnsValidJwt_ForUser()
    {
        var sut = new TokenService(Settings);
        var user = new User { Id = 7, Email = "a@b.com" };

        var token = sut.CreateToken(user);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Settings.Issuer,
            ValidAudience = Settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Settings.Key)),
        });
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(Role.Admin, "Admin")]
    [InlineData(Role.User, "User")]
    public void CreateToken_AddsRoleClaim_ForUserRole(Role role, string expected)
    {
        var token = new TokenService(Settings).CreateToken(new User { Id = 7, Email = "a@b.com", Role = role });

        var jwt = new JsonWebToken(token);

        Assert.Equal(expected, jwt.Claims.Single(c => c.Type == System.Security.Claims.ClaimTypes.Role).Value);
    }
}

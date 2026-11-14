using System.Security.Claims;
using System.Text;
using CryptoSimulator.Configuration;
using CryptoSimulator.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CryptoSimulator.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _settings;

        public TokenService(JwtSettings settings)
        {
            _settings = settings;
        }

        public string CreateToken(User user)
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                }),
                Issuer = _settings.Issuer,
                Audience = _settings.Audience,
                Expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                    SecurityAlgorithms.HmacSha256),
            };

            return new JsonWebTokenHandler().CreateToken(descriptor);
        }
    }
}

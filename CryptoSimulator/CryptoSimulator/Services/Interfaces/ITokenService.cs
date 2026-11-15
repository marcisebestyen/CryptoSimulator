using CryptoSimulator.Entities;

namespace CryptoSimulator.Services
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}

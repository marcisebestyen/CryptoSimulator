using CryptoSimulator.DTOs;

namespace CryptoSimulator.Services
{
    public interface ITradeService
    {
        Task<bool> BuyCrypto(int userId, int cryptoId, decimal amount);
        Task<bool> SellCrypto(int userId, int cryptoId, decimal amount);
        Task<PortfolioDto?> GetPortfolio(int userId);
        Task<IEnumerable<CryptoProfitLossDto>?> GetDetailedProfitLoss(int userId);
        Task<PortfolioProfitLossDto?> GetPortfolioProfitLoss(int userId);
    }
}

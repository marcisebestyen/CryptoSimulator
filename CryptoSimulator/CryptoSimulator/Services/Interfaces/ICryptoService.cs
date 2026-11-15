using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;

namespace CryptoSimulator.Services
{
    public interface ICryptoService
    {
        Task<IEnumerable<CryptoGetWithCurrentValueDto>> GetAllWithCurrentValueAsync();
        Task<bool> UpdateCryptoPriceAsync(int cryptoId, decimal newPrice, DateTime updateTimestamp);
        Task<PagedResult<CryptoPriceHistoryDto>> GetCryptoPriceHistoryAsync(int cryptoId, int page, int pageSize);
        Task<bool> HasOffersAsync(int cryptoId);
        Task<bool> IsNameTakenAsync(string name, int? excludeCryptoId = null);
        Task AddWithInitialPriceAsync(Crypto crypto, decimal initialPrice, DateTime timestamp);
    }
}

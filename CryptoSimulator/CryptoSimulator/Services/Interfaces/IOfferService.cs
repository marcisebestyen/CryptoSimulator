using CryptoSimulator.DTOs;
using CryptoSimulator.Results;

namespace CryptoSimulator.Services
{
    public interface IOfferService
    {
        Task<OfferResult> CreateAsync(int userId, CreateOfferDto dto, CancellationToken ct = default);
        Task<OfferResult> CancelAsync(int userId, int offerId, CancellationToken ct = default);
        Task<OfferResult> BuyAsync(int userId, int offerId, decimal amount, CancellationToken ct = default);
        Task<IReadOnlyList<MarketOfferDto>> GetMarketOffersAsync(int userId, CancellationToken ct = default);
        Task<IReadOnlyList<MyOfferDto>> GetMyOffersAsync(int userId, CancellationToken ct = default);
    }
}

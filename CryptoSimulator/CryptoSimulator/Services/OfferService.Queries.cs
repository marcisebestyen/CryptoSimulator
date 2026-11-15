using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;

namespace CryptoSimulator.Services
{
    public partial class OfferService
    {
        public async Task<IReadOnlyList<MarketOfferDto>> GetMarketOffersAsync(int userId, CancellationToken ct = default)
        {
            var wallet = await GetWalletAsync(userId);
            var ownWalletId = wallet?.Id ?? NoWalletId;
            var offers = (await _unitOfWork.OfferRepository.GetAsync(
                    o => (o.Status == OfferStatus.Open || o.Status == OfferStatus.PartlySold) && o.SellerWalletId != ownWalletId,
                    new[] { "Crypto", "SellerWallet.User" }))
                .OrderBy(o => o.CreatedAt)
                .ToList();
            if (offers.Count == 0)
            {
                return [];
            }

            var sellerIds = offers.Select(o => o.SellerWalletId).Distinct().ToList();
            var stats = await GetSellerStatsAsync(sellerIds);
            var rates = await GetCurrentRatesAsync(offers.Select(o => o.CryptoId).Distinct().ToList());

            return offers.Select(o =>
            {
                var seller = stats.GetValueOrDefault(o.SellerWalletId);
                return new MarketOfferDto
                {
                    Id = o.Id,
                    SellerUsername = o.SellerWallet.User.Username,
                    SellerCompletionRate = seller.Rate,
                    SellerTrades = seller.Trades,
                    CryptoId = o.CryptoId,
                    CryptoName = o.Crypto.Name,
                    RemainingAmount = o.RemainingAmount,
                    AskingPrice = o.AskingPrice,
                    CurrentRate = rates.GetValueOrDefault(o.CryptoId)
                };
            }).ToList();
        }

        public async Task<IReadOnlyList<MyOfferDto>> GetMyOffersAsync(int userId, CancellationToken ct = default)
        {
            var wallet = await GetWalletAsync(userId);
            if (wallet == null)
            {
                return [];
            }

            var offers = (await _unitOfWork.OfferRepository.GetAsync(o => o.SellerWalletId == wallet.Id, new[] { "Crypto" }))
                .OrderByDescending(o => o.CreatedAt)
                .ToList();
            var rates = await GetCurrentRatesAsync(offers.Select(o => o.CryptoId).Distinct().ToList());

            return offers.Select(o => ToMyOfferDto(o, rates.GetValueOrDefault(o.CryptoId))).ToList();
        }

        // Rate = offers with a sale / (offers with a sale + cancelled offers without any sale). Open offers count for neither.
        private async Task<Dictionary<int, (decimal? Rate, int Trades)>> GetSellerStatsAsync(List<int> sellerWalletIds)
        {
            var offers = await _unitOfWork.OfferRepository.GetAsync(o => sellerWalletIds.Contains(o.SellerWalletId));
            var fills = await _unitOfWork.OfferFillRepository.GetAsync(f => sellerWalletIds.Contains(f.Offer.SellerWalletId));
            var soldOfferIds = fills.Select(f => f.OfferId).ToHashSet();

            return sellerWalletIds.ToDictionary(id => id, id =>
            {
                var sellerOffers = offers.Where(o => o.SellerWalletId == id).ToList();
                var completed = sellerOffers.Count(o => soldOfferIds.Contains(o.Id));
                var failed = sellerOffers.Count(o => o.Status == OfferStatus.Cancelled && !soldOfferIds.Contains(o.Id));
                var trades = fills.Count(f => f.Offer.SellerWalletId == id);
                decimal? rate = completed + failed == 0 ? null : (decimal)completed / (completed + failed);
                return (rate, trades);
            });
        }
    }
}

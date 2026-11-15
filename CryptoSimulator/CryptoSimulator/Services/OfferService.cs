using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Results;

namespace CryptoSimulator.Services
{
    public partial class OfferService : IOfferService
    {
        private const int AmountDecimals = 4;
        private const int PriceDecimals = 8;
        private const decimal PriceLimit = 10_000_000_000m;
        private const int NoOfferId = 0;
        private const int NoWalletId = 0;
        private const decimal MaxBalance = 9_999_999_999_999_999.99m;
        private static readonly DateTime OpenEnded = new(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        private readonly IUnitOfWork _unitOfWork;

        public OfferService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<OfferResult> CreateAsync(int userId, CreateOfferDto dto, CancellationToken ct = default)
        {
            if (dto.Amount <= 0 || dto.Amount != Math.Round(dto.Amount, AmountDecimals))
            {
                return OfferResult.Invalid($"Amount must be positive with at most {AmountDecimals} decimals.");
            }

            if (dto.AskingPrice <= 0 || dto.AskingPrice >= PriceLimit
                || dto.AskingPrice != Math.Round(dto.AskingPrice, PriceDecimals))
            {
                return OfferResult.Invalid($"Asking price must be positive, below {PriceLimit} and have at most {PriceDecimals} decimals.");
            }

            var wallet = await GetWalletAsync(userId);
            if (wallet == null)
            {
                return OfferResult.NotFound("Wallet not found.");
            }

            var crypto = (await _unitOfWork.CryptoRepository.GetAsync(c => c.Id == dto.CryptoId)).FirstOrDefault();
            if (crypto == null)
            {
                return OfferResult.NotFound("Crypto not found.");
            }

            var currentRate = await GetCurrentRateAsync(dto.CryptoId);
            if (currentRate == null)
            {
                return OfferResult.Invalid("This crypto has no price yet.");
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
            // Offer 0 does not exist; only the wallet row is locked so two creates cannot spend the same coins.
            await _unitOfWork.LockOfferAndWalletsAsync(NoOfferId, wallet.Id, wallet.Id, ct);

            var holding = await GetHoldingAsync(wallet.Id, dto.CryptoId);
            if (holding == null || holding.Amount < dto.Amount)
            {
                return OfferResult.Invalid("You do not own enough of this crypto.");
            }

            holding.Amount -= dto.Amount;
            if (holding.Amount == 0)
            {
                await _unitOfWork.MyCryptosRepository.DeleteAsync(holding.WalletId, holding.CryptoId);
            }
            else
            {
                await _unitOfWork.MyCryptosRepository.UpdateAsync(holding);
            }

            var offer = new Offer
            {
                SellerWalletId = wallet.Id,
                CryptoId = crypto.Id,
                Crypto = crypto,
                Amount = dto.Amount,
                RemainingAmount = dto.Amount,
                AskingPrice = dto.AskingPrice,
                Status = OfferStatus.Open,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.OfferRepository.InsertAsync(offer);

            await _unitOfWork.SaveAsync();
            await _unitOfWork.CommitAsync(ct);

            return OfferResult.Success(ToMyOfferDto(offer, currentRate.Value));
        }

        public async Task<OfferResult> CancelAsync(int userId, int offerId, CancellationToken ct = default)
        {
            var firstRead = await GetOfferAsync(offerId);
            if (firstRead == null)
            {
                return OfferResult.NotFound("Offer not found.");
            }

            var wallet = await GetWalletAsync(userId);
            if (wallet == null || wallet.Id != firstRead.SellerWalletId)
            {
                return OfferResult.NotOwner();
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
            await _unitOfWork.LockOfferAndWalletsAsync(offerId, wallet.Id, wallet.Id, ct);

            // The lock detached the first read, so state may have changed while we waited.
            var offer = await GetOfferAsync(offerId);
            if (offer == null)
            {
                return OfferResult.NotFound("Offer not found.");
            }

            if (offer.Status is OfferStatus.SoldOut or OfferStatus.Cancelled)
            {
                return OfferResult.Conflict("Offer is already closed.");
            }

            var holding = await GetHoldingAsync(wallet.Id, offer.CryptoId);
            if (holding == null)
            {
                await _unitOfWork.MyCryptosRepository.InsertAsync(new MyCryptos
                {
                    WalletId = wallet.Id,
                    CryptoId = offer.CryptoId,
                    Amount = offer.RemainingAmount
                });
            }
            else
            {
                holding.Amount += offer.RemainingAmount;
                await _unitOfWork.MyCryptosRepository.UpdateAsync(holding);
            }

            offer.Status = OfferStatus.Cancelled;
            offer.ClosedAt = DateTime.UtcNow;
            await _unitOfWork.OfferRepository.UpdateAsync(offer);

            await _unitOfWork.SaveAsync();
            await _unitOfWork.CommitAsync(ct);

            var currentRate = await GetCurrentRateAsync(offer.CryptoId) ?? 0m;
            return OfferResult.Success(ToMyOfferDto(offer, currentRate));
        }

        public async Task<OfferResult> BuyAsync(int userId, int offerId, decimal amount, CancellationToken ct = default)
        {
            if (amount <= 0 || amount != Math.Round(amount, AmountDecimals))
            {
                return OfferResult.Invalid($"Amount must be positive with at most {AmountDecimals} decimals.");
            }

            var firstRead = await GetOfferAsync(offerId);
            if (firstRead == null)
            {
                return OfferResult.NotFound("Offer not found.");
            }

            var buyerWalletId = (await GetWalletAsync(userId))?.Id;
            if (buyerWalletId == null)
            {
                return OfferResult.NotFound("Wallet not found.");
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
            await _unitOfWork.LockOfferAndWalletsAsync(offerId, firstRead.SellerWalletId, buyerWalletId.Value, ct);

            // The lock detached the first read, so everything is loaded and checked again.
            var offer = await GetOfferAsync(offerId);
            if (offer == null)
            {
                return OfferResult.NotFound("Offer not found.");
            }

            var buyer = await GetWalletByIdAsync(buyerWalletId.Value);
            var seller = await GetWalletByIdAsync(offer.SellerWalletId);
            if (buyer == null || seller == null)
            {
                return OfferResult.NotFound("Wallet not found.");
            }

            if (buyer.Id == seller.Id)
            {
                return OfferResult.Invalid("You cannot buy your own offer.");
            }

            if (offer.Status is not (OfferStatus.Open or OfferStatus.PartlySold))
            {
                return OfferResult.Conflict("Offer is closed.");
            }

            if (amount > offer.RemainingAmount)
            {
                return OfferResult.Invalid("Amount exceeds what is left of the offer.");
            }

            var total = Math.Round(amount * offer.AskingPrice, 2, MidpointRounding.AwayFromZero);
            if (total <= 0)
            {
                return OfferResult.Invalid("Amount is too small to trade at this price.");
            }

            if (buyer.Balance < total)
            {
                return OfferResult.Invalid("Not enough cash.");
            }

            if (seller.Balance > MaxBalance - total)
            {
                return OfferResult.Invalid("The seller's balance cannot take this payment.");
            }

            buyer.Balance -= total;
            seller.Balance += total;
            await _unitOfWork.WalletRepository.UpdateAsync(buyer);
            await _unitOfWork.WalletRepository.UpdateAsync(seller);

            await AddBuyerCoinsAsync(buyer.Id, offer.CryptoId, amount);

            var now = DateTime.UtcNow;
            offer.RemainingAmount -= amount;
            offer.Status = offer.RemainingAmount == 0 ? OfferStatus.SoldOut : OfferStatus.PartlySold;
            if (offer.Status == OfferStatus.SoldOut)
            {
                offer.ClosedAt = now;
            }

            await _unitOfWork.OfferRepository.UpdateAsync(offer);

            await _unitOfWork.TransactionRepository.InsertAsync(NewTransaction(seller.Id, offer, amount, false, now));
            await _unitOfWork.TransactionRepository.InsertAsync(NewTransaction(buyer.Id, offer, amount, true, now));
            await _unitOfWork.OfferFillRepository.InsertAsync(new OfferFill
            {
                OfferId = offer.Id,
                BuyerWalletId = buyer.Id,
                Amount = amount,
                Price = offer.AskingPrice,
                Date = now
            });

            await _unitOfWork.SaveAsync();
            await _unitOfWork.CommitAsync(ct);

            return OfferResult.Success();
        }

        private async Task<Wallet?> GetWalletAsync(int userId)
        {
            return (await _unitOfWork.WalletRepository.GetAsync(w => w.UserId == userId)).FirstOrDefault();
        }

        private async Task<decimal?> GetCurrentRateAsync(int cryptoId)
        {
            var rates = await GetCurrentRatesAsync(new List<int> { cryptoId });
            return rates.TryGetValue(cryptoId, out var rate) ? rate : null;
        }

        private async Task<Dictionary<int, decimal>> GetCurrentRatesAsync(List<int> cryptoIds)
        {
            // Only the open-ended log is the current price, so the database returns one row per crypto.
            var logs = await _unitOfWork.CryptoLogRepository.GetAsync(cl => cryptoIds.Contains(cl.CryptoId) && cl.To == OpenEnded);
            return logs
                .GroupBy(cl => cl.CryptoId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(cl => cl.To).First().CurrentValue);
        }

        private async Task<Wallet?> GetWalletByIdAsync(int walletId)
        {
            return (await _unitOfWork.WalletRepository.GetAsync(w => w.Id == walletId)).FirstOrDefault();
        }

        private async Task AddBuyerCoinsAsync(int walletId, int cryptoId, decimal amount)
        {
            var holding = await GetHoldingAsync(walletId, cryptoId);
            if (holding == null)
            {
                await _unitOfWork.MyCryptosRepository.InsertAsync(new MyCryptos
                {
                    WalletId = walletId,
                    CryptoId = cryptoId,
                    Amount = amount
                });
                return;
            }

            holding.Amount += amount;
            await _unitOfWork.MyCryptosRepository.UpdateAsync(holding);
        }

        private static Transactions NewTransaction(int walletId, Offer offer, decimal amount, bool isPurchase, DateTime date)
        {
            return new Transactions
            {
                WalletId = walletId,
                CryptoId = offer.CryptoId,
                Amount = amount,
                ExchangeRate = offer.AskingPrice,
                Date = date,
                IsPurchase = isPurchase
            };
        }

        private async Task<Offer?> GetOfferAsync(int offerId)
        {
            return (await _unitOfWork.OfferRepository.GetAsync(o => o.Id == offerId, new[] { "Crypto" })).FirstOrDefault();
        }

        private async Task<MyCryptos?> GetHoldingAsync(int walletId, int cryptoId)
        {
            return (await _unitOfWork.MyCryptosRepository
                .GetAsync(mc => mc.WalletId == walletId && mc.CryptoId == cryptoId)).FirstOrDefault();
        }

        private static MyOfferDto ToMyOfferDto(Offer offer, decimal currentRate)
        {
            return new MyOfferDto
            {
                Id = offer.Id,
                CryptoId = offer.CryptoId,
                CryptoName = offer.Crypto.Name,
                Amount = offer.Amount,
                RemainingAmount = offer.RemainingAmount,
                SoldAmount = offer.Amount - offer.RemainingAmount,
                AskingPrice = offer.AskingPrice,
                CurrentRate = currentRate,
                Status = offer.Status,
                CreatedAt = offer.CreatedAt
            };
        }
    }
}

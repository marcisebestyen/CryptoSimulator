using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Results;

namespace CryptoSimulator.Services
{
    public interface IDepositService
    {
        Task<DepositOutcome> DepositAsync(DepositCreateDto dto);
        Task<IReadOnlyList<DepositHistoryDto>> GetDepositsAsync(int userId);
        Task<IReadOnlyList<SavedCardGetDto>> GetCardsAsync(int userId);
        Task<bool> DeleteCardAsync(int userId, int cardId);
    }

    public class DepositService : IDepositService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DepositService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DepositOutcome> DepositAsync(DepositCreateDto dto)
        {
            if (!CardRules.IsValidAmount(dto.Amount))
            {
                return DepositOutcome.Invalid("Amount must be between 10.00 and 100000.00 with at most 2 decimals.");
            }

            var hasNewCard = dto.CardNumber is not null || dto.HolderName is not null
                || dto.ExpiryMonth is not null || dto.ExpiryYear is not null;
            if ((dto.SavedCardId is not null) == hasNewCard)
            {
                return DepositOutcome.Invalid("Provide either a saved card or new card details.");
            }

            if (!CardRules.IsValidCvv(dto.Cvv))
            {
                return DepositOutcome.Invalid("CVV must be 3 or 4 digits.");
            }

            var cardResult = dto.SavedCardId is int savedId
                ? await FindSavedCardAsync(dto.UserId, savedId)
                : await BuildNewCardAsync(dto);
            if (cardResult.Outcome is not null)
            {
                return cardResult.Outcome;
            }

            var wallet = _unitOfWork.WalletRepository.Get(w => w.UserId == dto.UserId).FirstOrDefault();
            if (wallet is null)
            {
                return DepositOutcome.Invalid("Wallet not found.");
            }

            var card = cardResult.Card!;
            var deposit = new Deposit
            {
                WalletId = wallet.Id,
                Last4 = card.Last4,
                Network = card.Network,
                Amount = dto.Amount,
                Date = DateTime.UtcNow
            };

            if (card.Id != 0)
            {
                deposit.SavedCardId = card.Id;
            }
            else if (dto.SaveCard)
            {
                await _unitOfWork.SavedCardRepository.InsertAsync(card);
                deposit.SavedCard = card;
            }

            wallet.Balance += dto.Amount;
            await _unitOfWork.WalletRepository.UpdateAsync(wallet);
            await _unitOfWork.DepositRepository.InsertAsync(deposit);
            await _unitOfWork.SaveAsync();

            return DepositOutcome.Success(new DepositGetDto
            {
                Id = deposit.Id,
                Amount = deposit.Amount,
                Last4 = deposit.Last4,
                Network = deposit.Network.ToString(),
                Date = deposit.Date,
                NewBalance = wallet.Balance
            });
        }

        public async Task<IReadOnlyList<DepositHistoryDto>> GetDepositsAsync(int userId)
        {
            var wallet = _unitOfWork.WalletRepository.Get(w => w.UserId == userId).FirstOrDefault();
            if (wallet is null)
            {
                return [];
            }

            var deposits = await _unitOfWork.DepositRepository.GetAsync(d => d.WalletId == wallet.Id);
            return deposits
                .OrderByDescending(d => d.Date)
                .ThenByDescending(d => d.Id)
                .Select(d => new DepositHistoryDto
                {
                    Id = d.Id,
                    Amount = d.Amount,
                    Last4 = d.Last4,
                    Network = d.Network.ToString(),
                    Date = d.Date
                })
                .ToList();
        }

        public async Task<IReadOnlyList<SavedCardGetDto>> GetCardsAsync(int userId)
        {
            var cards = await _unitOfWork.SavedCardRepository.GetAsync(c => c.UserId == userId);
            return cards
                .Select(c => new SavedCardGetDto
                {
                    Id = c.Id,
                    HolderName = c.HolderName,
                    Last4 = c.Last4,
                    Network = c.Network.ToString(),
                    ExpiryMonth = c.ExpiryMonth,
                    ExpiryYear = c.ExpiryYear
                })
                .ToList();
        }

        public async Task<bool> DeleteCardAsync(int userId, int cardId)
        {
            var card = (await _unitOfWork.SavedCardRepository.GetAsync(c => c.Id == cardId && c.UserId == userId))
                .FirstOrDefault();
            if (card is null)
            {
                return false;
            }

            await _unitOfWork.SavedCardRepository.DeleteAsync(cardId);
            await _unitOfWork.SaveAsync();
            return true;
        }

        private async Task<CardResult> FindSavedCardAsync(int userId, int cardId)
        {
            var card = (await _unitOfWork.SavedCardRepository.GetAsync(c => c.Id == cardId && c.UserId == userId))
                .FirstOrDefault();
            if (card is null)
            {
                return CardResult.Fail(DepositOutcome.CardNotFound());
            }

            return CardRules.IsExpiryValid(card.ExpiryMonth, card.ExpiryYear, DateTime.UtcNow)
                ? CardResult.Ok(card)
                : CardResult.Fail(DepositOutcome.Invalid("Saved card has expired."));
        }

        private async Task<CardResult> BuildNewCardAsync(DepositCreateDto dto)
        {
            var number = (dto.CardNumber ?? string.Empty).Replace(" ", string.Empty);
            var holder = dto.HolderName?.Trim();
            if (string.IsNullOrEmpty(holder))
            {
                return CardResult.Fail(DepositOutcome.Invalid("Card holder name is required."));
            }

            if (holder.Length > 100)
            {
                return CardResult.Fail(DepositOutcome.Invalid("Card holder name must be at most 100 characters."));
            }

            if (!CardRules.IsValidNumber(number))
            {
                return CardResult.Fail(DepositOutcome.Invalid("Card number is invalid."));
            }

            if (dto.ExpiryMonth is not int month || dto.ExpiryYear is not int year
                || !CardRules.IsExpiryValid(month, year, DateTime.UtcNow))
            {
                return CardResult.Fail(DepositOutcome.Invalid("Card expiry is invalid."));
            }

            var last4 = number[^4..];
            var existing = dto.SaveCard
                ? (await _unitOfWork.SavedCardRepository.GetAsync(c =>
                        c.UserId == dto.UserId && c.Last4 == last4 && c.ExpiryMonth == month
                        && c.ExpiryYear == year && c.HolderName == holder))
                    .FirstOrDefault()
                : null;
            if (existing is not null)
            {
                return CardResult.Ok(existing);
            }

            return CardResult.Ok(new SavedCard
            {
                UserId = dto.UserId,
                HolderName = holder,
                Last4 = last4,
                Network = CardRules.DetectNetwork(number),
                ExpiryMonth = month,
                ExpiryYear = year
            });
        }

        private sealed record CardResult(SavedCard? Card, DepositOutcome? Outcome)
        {
            public static CardResult Ok(SavedCard card) => new(card, null);
            public static CardResult Fail(DepositOutcome outcome) => new(null, outcome);
        }
    }
}

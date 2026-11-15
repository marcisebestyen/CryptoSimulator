using CryptoSimulator.DTOs;
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
}

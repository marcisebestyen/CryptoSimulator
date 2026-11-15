using CryptoSimulator.DTOs;

namespace CryptoSimulator.Services
{
    public interface ITransactionService
    {
        Task<IEnumerable<TransactionsGetDto>> GetUserTransactionsAsync(int userId, bool newestFirst = true);
    }
}

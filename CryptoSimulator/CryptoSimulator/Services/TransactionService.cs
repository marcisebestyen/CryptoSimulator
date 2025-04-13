using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Repositories;

namespace CryptoSimulator.Services
{
    public interface ITransactionService
    {
        Task<IEnumerable<TransactionsGetDto>> GetUserTransactionsAsync(int userId, bool newestFirst = true);
    }

    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TransactionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TransactionsGetDto>> GetUserTransactionsAsync(int userId, bool newestFirst = true)
        {
            var wallet = (await _unitOfWork.WalletRepository.GetAsync(w => w.UserId == userId)).FirstOrDefault();
            if (wallet == null)
            {
                return null;
            }

            var transactions = await _unitOfWork.TransactionRepository.GetAsync(t => t.WalletId == wallet.Id, new[] { "Crypto" });

            var orderedTransactions = newestFirst ? transactions.OrderByDescending(t => t.Date) : transactions.OrderBy(t => t.Date);

            var transactionDtos = _mapper.Map<IEnumerable<TransactionsGetDto>>(orderedTransactions);
            return transactionDtos;
        }
    }
}

using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;

namespace CryptoSimulator.Services
{
    public interface ICryptoService
    {
        Task<bool> BuyCrypto(int userId, int cryptoId, decimal amount);
        Task<bool> SellCrypto(int userId, int cryptoId, decimal amount);
    }

    public class CryptoService : ICryptoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CryptoService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<bool> BuyCrypto(int userId, int cryptoId, decimal amount)
        {
            var wallet = _unitOfWork.WalletRepository
                .Get(u => u.UserId == userId)
                .FirstOrDefault();
            if (wallet == null)
            {
                return false; 
            }

            var crypto = _unitOfWork.CryptoRepository
                .Get(c => c.Id == cryptoId)
                .FirstOrDefault();
            if (crypto == null)
            {
                return false;
            }

            var cryptoLog = _unitOfWork.CryptoLogRepository
                .Get(cl => cl.CryptoId == cryptoId)
                .OrderByDescending(cl => cl.To)
                .FirstOrDefault();
            if (cryptoLog == null)
            {
                return false;
            }

            decimal totalPrice = amount * cryptoLog.CurrentValue;

            if (wallet.Balance < totalPrice)
            {
                return false;
            }

            wallet.Balance -= totalPrice;
            await _unitOfWork.WalletRepository.UpdateAsync(wallet);

            var transactionPostDto = new TransactionsPostDto
            {
                WalletId = wallet.Id,
                CryptoId = crypto.Id,
                Amount = amount,
                ExchangeRate = cryptoLog.CurrentValue,
                IsPurchase = true
            };
            var transaction = _mapper.Map<Transactions>(transactionPostDto);
            await _unitOfWork.TransactionRepository.InsertAsync(transaction);


            var myCrypto = _unitOfWork.MyCryptosRepository
                .Get(mc => mc.CryptoId == cryptoId && mc.WalletId == wallet.Id)
                .FirstOrDefault();
            if (myCrypto == null)
            {
                var myCryptoPostDto = new MyCryptosDto
                {
                    WalletId = wallet.Id,
                    CryptoId = crypto.Id,
                    Amount = amount
                };
                myCrypto = _mapper.Map<MyCryptos>(myCryptoPostDto);
                await _unitOfWork.MyCryptosRepository.InsertAsync(myCrypto);
            }
            else
            {
                myCrypto.Amount += amount;
                await _unitOfWork.MyCryptosRepository.UpdateAsync(myCrypto);
            }

            await _unitOfWork.SaveAsync();
            return true;
        }

        public async Task<bool> SellCrypto(int userId, int cryptoId, decimal amount)
        {
            var wallet = _unitOfWork.WalletRepository
                .Get(w => w.UserId == userId)
                .FirstOrDefault();
            if (wallet == null)
            {
                return false;
            }

            var crypto = _unitOfWork.CryptoRepository
                .Get(c => c.Id == cryptoId)
                .FirstOrDefault();
            if (crypto == null)
            {
                return false;
            }

            var cryptoLog = _unitOfWork.CryptoLogRepository
                .Get(cl => cl.CryptoId == cryptoId)
                .OrderByDescending(cl => cl.To)
                .FirstOrDefault();
            if (cryptoLog == null)
            {
                return false;
            }

            var myCrypto = _unitOfWork.MyCryptosRepository
                .Get(mc => mc.CryptoId == cryptoId && mc.WalletId == wallet.Id)
                .FirstOrDefault();
            if (myCrypto == null || myCrypto.Amount < amount)
            {
                return false;
            }

            decimal totalPrice = amount * cryptoLog.CurrentValue;

            wallet.Balance += totalPrice;
            await _unitOfWork.WalletRepository.UpdateAsync(wallet);

            var transactionPostDto = new TransactionsPostDto
            {
                WalletId = wallet.Id,
                CryptoId = crypto.Id,
                Amount = amount,
                ExchangeRate = cryptoLog.CurrentValue,
                IsPurchase = false
            };
            var transaction = _mapper.Map<Transactions>(transactionPostDto);
            await _unitOfWork.TransactionRepository.InsertAsync(transaction);

            myCrypto.Amount -= amount;
            if (myCrypto.Amount == 0)
            {
                await _unitOfWork.MyCryptosRepository.DeleteAsync(myCrypto.CryptoId);
            }
            else
            {
                await _unitOfWork.MyCryptosRepository.UpdateAsync(myCrypto);
            }

            await _unitOfWork.SaveAsync();
            return true;
        }
    }
}

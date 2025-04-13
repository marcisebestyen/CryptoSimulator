using AutoMapper;
using Azure.Core;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;

namespace CryptoSimulator.Services
{
    public interface ITradeService
    {
        Task<bool> BuyCrypto(int userId, int cryptoId, decimal amount);
        Task<bool> SellCrypto(int userId, int cryptoId, decimal amount);
        Task<PortfolioDto?> GetPortfolio(int userId);
        Task<IEnumerable<CryptoProfitLossDto>?> GetDetailedProfitLossAsync(int userId);
        Task<PortfolioProfitLossDto?> GetPortfolioProfitLossAsync(int userId);
    }

    public class TradeService : ITradeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private static readonly DateTime MaxDateTimeValue = new DateTime(9999, 12, 31, 23, 59, 59);

        public TradeService(IUnitOfWork unitOfWork, IMapper mapper)
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

        public async Task<PortfolioDto?> GetPortfolio(int userId)
        {
            var wallet = (await _unitOfWork.WalletRepository.GetAsync(w => w.UserId == userId)).FirstOrDefault();
            if (wallet == null)
            {
                return null;
            }

            var holdings = await _unitOfWork.MyCryptosRepository.GetAsync(mc => mc.WalletId == wallet.Id && mc.Amount > 0, new[] { "Crypto" });

            var portfolio = new PortfolioDto
            {
                UserId = userId,
                FiatBalance = wallet.Balance,
                Holdings = new List<PortfolioItemDto>()
            };

            if (!holdings.Any())
            {
                portfolio.TotalCryptoValue = 0;
                portfolio.TotalPortfolioValue = portfolio.FiatBalance;
                return portfolio;
            }

            var heldCryptoIds = holdings.Select(h => h.CryptoId).Distinct().ToList();

            DateTime maxDateTime = new DateTime(9999, 12, 31, 23, 59, 59);
            var currentLogs = await _unitOfWork.CryptoLogRepository.GetAsync(cl => heldCryptoIds.Contains(cl.CryptoId) && cl.To == maxDateTime);
            var rateLookup = currentLogs.ToDictionary(cl => cl.CryptoId, cl => cl.CurrentValue);

            decimal totalCryptoValue = 0m;
            foreach (var holding in holdings)
            {
                if (holding.Crypto == null)
                {
                    continue;
                }

                decimal currentRate = rateLookup.TryGetValue(holding.CryptoId, out var rate) ? rate : 0m;
                decimal currentValue = holding.Amount * currentRate;

                portfolio.Holdings.Add(new PortfolioItemDto
                {
                    CryptoId = holding.CryptoId,
                    CryptoName = holding.Crypto.Name,
                    AmountOwned = holding.Amount,
                    CurrentExchangeRate = currentRate,
                    CurrentValue = currentValue
                });

                totalCryptoValue += currentValue;
            }

            portfolio.TotalCryptoValue = totalCryptoValue;
            portfolio.TotalPortfolioValue = portfolio.FiatBalance + totalCryptoValue;

            return portfolio;
        }

        public async Task<IEnumerable<CryptoProfitLossDto>?> GetDetailedProfitLossAsync(int userId)
        {
            var wallet = (await _unitOfWork.WalletRepository.GetAsync(w => w.UserId == userId)).FirstOrDefault();
            if (wallet == null)
            {
                return null;
            }

            var holdings = await _unitOfWork.MyCryptosRepository.GetAsync(mc => mc.WalletId == wallet.Id && mc.Amount > 0, new[] { "Crypto" });
            if (!holdings.Any())
            {
                return Enumerable.Empty<CryptoProfitLossDto>();
            }

            var heldCryptoIds = holdings.Select(h => h.CryptoId).Distinct().ToList();
            var currentHoldings = await _unitOfWork.CryptoLogRepository.GetAsync(cl => heldCryptoIds.Contains(cl.CryptoId) && cl.To == MaxDateTimeValue);
            var rateLookup = currentHoldings.ToDictionary(cl => cl.CryptoId, cl => cl.CurrentValue);

            var results = new List<CryptoProfitLossDto>();
            foreach (var holding in holdings)
            {
                if (holding.Crypto == null)
                {
                    continue;
                }

                decimal currentRate = rateLookup.TryGetValue(holding.CryptoId, out var rate) ? rate : 0m;
                decimal avgPurchasePrice = await CalculateAvgPurchasePriceAsync(wallet.Id, holding.CryptoId);
                decimal amountOwned = holding.Amount;
                decimal costBasis = amountOwned * avgPurchasePrice;
                decimal currentValue = amountOwned * currentRate;
                decimal profitLossAmount = currentValue - costBasis;
                decimal profitLossPercentage = (costBasis == 0) ? 0m : (profitLossAmount / costBasis) * 100m;

                results.Add(new CryptoProfitLossDto
                {
                    CryptoId = holding.CryptoId,
                    CryptoName = holding.Crypto.Name,
                    AmountOwned = amountOwned,
                    AvgPurchasePrice = avgPurchasePrice,
                    CurrentExchangeRate = currentRate,
                    TotalCostBasis = costBasis,
                    CurrentValue = currentValue,
                    ProfitLossAmount = profitLossAmount,
                    ProfitLossPercentage = Math.Round(profitLossPercentage, 2)
                });
            }

            return results;
        }

        public async Task<PortfolioProfitLossDto?> GetPortfolioProfitLossAsync(int userId)
        {
            var detailedProfitLoss = await GetDetailedProfitLossAsync(userId);
            if (detailedProfitLoss == null)
            {
                //Console.WriteLine($"DEBUG: GetDetailedProfitLossAsync returned null for UserId: {userId}");
                return null;
            }

            var detailList = detailedProfitLoss.ToList();

            if (!detailList.Any())
            {
                //Console.WriteLine($"DEBUG: GetDetailedProfitLossAsync returned null for UserId: {userId}");
                return new PortfolioProfitLossDto { UserId = userId };
            }

            //Console.WriteLine($"DEBUG: Found {detailList.Count} detailed items for UserId: {userId}");

            decimal totalCost = detailList.Sum(d => d.TotalCostBasis);
            decimal totalValue = detailList.Sum(d => d.CurrentValue);

            //Console.WriteLine($"DEBUG: Calculated totals for UserId {userId}: Cost={totalCost}, Value={totalValue}");


            decimal totalProfitLossAmount = totalValue - totalCost;
            decimal totalProfitLossPercentage = (totalCost == 0) ? 0m : (totalProfitLossAmount / totalCost) * 100m;

            var summaryResult = new PortfolioProfitLossDto
            {
                UserId = userId,
                TotalCurrentCryptoValue = totalValue,
                TotalCostBasis = totalCost,
                TotalProfitLossAmount = totalProfitLossAmount,
                TotalProfitLossPercentage = Math.Round(totalProfitLossPercentage, 2)
            };

            //Console.WriteLine($"DEBUG: Returning Summary DTO for UserId {userId}: Cost={summaryResult.TotalCostBasis}, Value={summaryResult.TotalCurrentCryptoValue}");

            return summaryResult;
        }

        private async Task<decimal> CalculateAvgPurchasePriceAsync(int walletId, int cryptoId)
        {
            var purchaseTransactions = await _unitOfWork.TransactionRepository.GetAsync(t => t.WalletId == walletId && t.CryptoId == cryptoId && t.IsPurchase);
            if (!purchaseTransactions.Any())
            {
                return 0m;
            }

            decimal totalCost = 0m;
            decimal totalAmount = 0m;

            foreach (var t in purchaseTransactions)
            {
                totalCost += t.Amount * t.ExchangeRate;
                totalAmount += t.Amount;
            }

            if (totalAmount == 0)
            {
                return 0m;
            }

            return totalCost / totalAmount;
        }
    }
}

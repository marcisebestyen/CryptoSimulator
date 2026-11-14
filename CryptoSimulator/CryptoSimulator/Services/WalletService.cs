using CryptoSimulator.DTOs;
using CryptoSimulator.Repositories;

namespace CryptoSimulator.Services
{
    public interface IWalletService
    {
        Task<UserWalletDetailsDto?> GetUserWalletDetailsAsync(int userId);
    }

    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WalletService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<UserWalletDetailsDto?> GetUserWalletDetailsAsync(int userId)
        {
            var wallet = (await _unitOfWork.WalletRepository.GetAsync(w => w.UserId == userId)).FirstOrDefault();

            if (wallet == null)
            {
                return null;
            }

            var holdings = await _unitOfWork.MyCryptosRepository.GetAsync(mc => mc.WalletId == wallet.Id && mc.Amount > 0, new[] { "Crypto" });

            var ownedCryptoIds = holdings.Select(h => h.CryptoId).Distinct().ToList();
            var maxDateTime = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            var currentLogs = await _unitOfWork.CryptoLogRepository.GetAsync(cl => ownedCryptoIds.Contains(cl.CryptoId) && cl.To == maxDateTime);
            var priceLookup = currentLogs.ToDictionary(cl => cl.CryptoId, cl => cl.CurrentValue);

            var resultDto = new UserWalletDetailsDto
            {
                UserId = userId,
                FiatBalance = wallet.Balance,
                OwnedCryptos = new List<OwnedCryptoDto>()
            };

            foreach (var holding in holdings)
            {
                if (holding.Crypto != null)
                {
                    var price = priceLookup.TryGetValue(holding.CryptoId, out var p) ? p : 0m;
                    resultDto.OwnedCryptos.Add(new OwnedCryptoDto
                    {
                        CryptoId = holding.Crypto.Id,
                        CryptoName = holding.Crypto.Name,
                        Amount = holding.Amount,
                        CurrentPrice = price,
                        CurrentValue = holding.Amount * price
                    });
                }
            }

            return resultDto;
        }
    }
}

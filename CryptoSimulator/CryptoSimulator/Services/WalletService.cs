using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Repositories;
using System.Numerics;

namespace CryptoSimulator.Services
{
    public interface IWalletService
    {
        Task<UserWalletDetailsDto?> GetUserWalletDetailsAsync(int userId);
    }

    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public WalletService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<UserWalletDetailsDto?> GetUserWalletDetailsAsync(int userId)
        {
            var wallet = (await _unitOfWork.WalletRepository.GetAsync(w => w.UserId == userId)).FirstOrDefault();

            if (wallet == null)
            {
                return null;
            }

            var holdings = await _unitOfWork.MyCryptosRepository.GetAsync(mc => mc.WalletId == wallet.Id && mc.Amount > 0, new[] { "Crypto" });

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
                    resultDto.OwnedCryptos.Add(new OwnedCryptoDto
                    {
                        CryptoId = holding.Crypto.Id,
                        CryptoName = holding.Crypto.Name,
                        Amount = holding.Amount
                    });
                }
            }

            return resultDto;
        }
    }
}

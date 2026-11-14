using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/wallets")]
    public class WalletController : AuthorizedControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IWalletService _walletService;

        public WalletController(IUnitOfWork unitOfWork, IMapper mapper, IWalletService walletService) : base(unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _walletService = walletService;
        }

        [HttpGet("~/api/users/{userId:int}/wallet")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserWalletDetailsDto))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserWalletDetailsDto>> GetUserWallet(int userId)
        {
            var denied = await DenyUnlessOwnerOrAdminAsync(userId);
            if (denied != null)
            {
                return denied;
            }

            var walletDetails = await _walletService.GetUserWalletDetailsAsync(userId);

            if (walletDetails == null)
            {
                return NotFound($"Wallet details for User ID {userId} not found.");
            }

            return Ok(walletDetails);
        }

        [HttpPut("{walletId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WalletGetDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WalletGetDto>> UpdateWallet(int walletId, [FromBody] WalletPutDto dto)
        {
            var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(new object[] { walletId }, null, null);
            if (wallet == null)
            {
                return NotFound($"Wallet with ID {walletId} not found.");
            }
            if (!IsOwnerOrAdmin(wallet.UserId))
            {
                return Forbid();
            }
            _mapper.Map(dto, wallet);
            await _unitOfWork.WalletRepository.UpdateAsync(wallet);
            await _unitOfWork.SaveAsync();
            return Ok(_mapper.Map<WalletGetDto>(wallet));
        }

        [HttpDelete("{walletId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteWallet(int walletId)
        {
            var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(new object[] { walletId }, null, null);
            if (wallet == null)
            {
                return NotFound($"Wallet with ID {walletId} not found.");
            }
            if (!IsOwnerOrAdmin(wallet.UserId))
            {
                return Forbid();
            }
            await _unitOfWork.WalletRepository.DeleteAsync(walletId);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }
    }
}

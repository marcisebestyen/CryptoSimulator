using CryptoSimulator.DTOs;
using CryptoSimulator.Repositories;
using CryptoSimulator.Results;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Authorize]
    public class DepositController : AuthorizedControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDepositService _depositService;

        public DepositController(IUnitOfWork unitOfWork, IDepositService depositService) : base(unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _depositService = depositService;
        }

        private async Task<bool> UserExistsAsync(int userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(new object[] { userId }, null, null);
            return user != null;
        }

        // Owner check, then user-exists check. Returns null when the caller may continue.
        private async Task<ActionResult?> GuardAsync(int userId)
        {
            var denied = await DenyUnlessOwnerAsync(userId);
            if (denied != null)
            {
                return denied;
            }

            return await UserExistsAsync(userId) ? null : NotFound($"User with ID {userId} not found.");
        }

        [HttpPost("api/deposits")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DepositGetDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DepositGetDto>> CreateDeposit([FromBody] DepositCreateDto dto)
        {
            var denied = await GuardAsync(dto.UserId);
            if (denied != null)
            {
                return denied;
            }

            var outcome = await _depositService.DepositAsync(dto);
            return outcome.Status switch
            {
                DepositStatus.Success => Created($"/api/users/{dto.UserId}/deposits", outcome.Deposit),
                DepositStatus.CardNotFound => NotFound(outcome.Error),
                _ => BadRequest(outcome.Error)
            };
        }

        [HttpGet("api/users/{userId:int}/deposits")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<DepositHistoryDto>))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<DepositHistoryDto>>> GetDeposits(int userId)
        {
            var denied = await GuardAsync(userId);
            if (denied != null)
            {
                return denied;
            }

            return Ok(await _depositService.GetDepositsAsync(userId));
        }

        [HttpGet("api/users/{userId:int}/cards")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<SavedCardGetDto>))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<SavedCardGetDto>>> GetCards(int userId)
        {
            var denied = await GuardAsync(userId);
            if (denied != null)
            {
                return denied;
            }

            return Ok(await _depositService.GetCardsAsync(userId));
        }

        [HttpDelete("api/users/{userId:int}/cards/{cardId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteCard(int userId, int cardId)
        {
            var denied = await GuardAsync(userId);
            if (denied != null)
            {
                return denied;
            }

            return await _depositService.DeleteCardAsync(userId, cardId)
                ? NoContent()
                : NotFound($"Card with ID {cardId} not found.");
        }
    }
}

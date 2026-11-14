using CryptoSimulator.DTOs;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Route("api/trades")]
    public class TradeController : ControllerBase
    {
        IUnitOfWork _unitOfWork;
        ITradeService _tradeService;

        public TradeController(IUnitOfWork unitOfWork, ITradeService tradeService)
        {
            _unitOfWork = unitOfWork;
            _tradeService = tradeService;
        }

        private async Task<bool> UserExistsAsync(int userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(new object[] { userId }, null, null);
            return user != null;
        }

        [HttpPost("buy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> BuyCrypto([FromBody] BuyCryptoDto buyDto)
        {
            if (!await UserExistsAsync(buyDto.UserId))
            {
                return NotFound($"User with ID {buyDto.UserId} not found.");
            }

            var result = await _tradeService.BuyCrypto(buyDto.UserId, buyDto.CryptoId, buyDto.Amount);
            if (result)
            {
                return Ok("Crypto purchased successfully");
            }
            return BadRequest("Transaction failed");
        }

        [HttpPost("sell")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SellCrypto([FromBody] SellCryptoDto sellDto)
        {
            if (!await UserExistsAsync(sellDto.UserId))
            {
                return NotFound($"User with ID {sellDto.UserId} not found.");
            }

            var result = await _tradeService.SellCrypto(sellDto.UserId, sellDto.CryptoId, sellDto.Amount);
            if (result)
            {
                return Ok("Crypto sold successfully");
            }
            return BadRequest("Transaction failed");
        }

        [HttpGet("~/api/users/{userId:int}/portfolio")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PortfolioDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PortfolioDto>> GetPortfolio(int userId)
        {
            if (!await UserExistsAsync(userId))
            {
                return NotFound($"User with ID {userId} not found.");
            }

            var portfolio = await _tradeService.GetPortfolio(userId);
            if (portfolio == null)
            {
                return NotFound($"Portfolio data for User ID {userId}");
            }
            return Ok(portfolio);
        }

        [HttpGet("~/api/users/{userId:int}/portfolio/profit-loss")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CryptoProfitLossDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<CryptoProfitLossDto>>> GetDetailedProfitLoss(int userId)
        {
            if (!await UserExistsAsync(userId))
            {
                return NotFound($"User with ID {userId} not found.");
            }

            var result = await _tradeService.GetDetailedProfitLoss(userId);
            if (result == null)
            {
                return NotFound($"User or Wallet not found for User ID {userId}.");
            }
            return Ok(result);
        }

        [HttpGet("~/api/users/{userId:int}/portfolio/profit-loss/summary")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PortfolioProfitLossDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PortfolioProfitLossDto>> GetPortfolioProfitLossSummary(int userId)
        {
            if (!await UserExistsAsync(userId))
            {
                return NotFound($"User with ID {userId} not found.");
            }

            var result = await _tradeService.GetPortfolioProfitLoss(userId);
            if (result == null)
            {
                return NotFound($"User or Wallet not found for User ID {userId}.");
            }
            return Ok(result);
        }
    }
}

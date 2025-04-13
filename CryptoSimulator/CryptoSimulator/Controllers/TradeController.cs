using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Controllers
{
    public class TradeController : ControllerBase
    {
        IUnitOfWork _unitOfWork;
        IMapper _mapper;
        ITradeService _tradeService;

        public TradeController(IUnitOfWork unitOfWork, IMapper mapper, ITradeService tradeService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tradeService = tradeService;
        }

        [HttpPost("buy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> BuyCrypto([FromBody] BuyCryptoDto buyDto)
        {
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
        public async Task<ActionResult> SellCrypto([FromBody] SellCryptoDto sellDto)
        {
            var result = await _tradeService.SellCrypto(sellDto.UserId, sellDto.CryptoId, sellDto.Amount);
            if (result)
            {
                return Ok("Crypto sold successfully");
            }
            return BadRequest("Transaction failed");
        }

        [HttpGet("portfolio/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PortfolioDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PortfolioDto>> GetPortfolio(int userId)
        {
            var portfolio = await _tradeService.GetPortfolio(userId);
            if (portfolio == null)
            {
                return NotFound($"Portfolio data for User ID {userId}");
            }
            return Ok(portfolio);
        }

        [HttpGet("portfolio/{userId}/profitloss/details")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CryptoProfitLossDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<CryptoProfitLossDto>>> GetDetailedProfitLoss(int userId)
        {
            var result = await _tradeService.GetDetailedProfitLossAsync(userId);
            if (result == null)
            {
                return NotFound($"User or Wallet not found for User ID {userId}.");
            }
            return Ok(result);
        }

        [HttpGet("portfolio/{userId}/profitloss/summary")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PortfolioProfitLossDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PortfolioProfitLossDto>> GetPortfolioProfitLossSummary(int userId)
        {
            var result = await _tradeService.GetPortfolioProfitLossAsync(userId);
            if (result == null)
            {
                return NotFound($"User or Wallet not found for User ID {userId}.");
            }
            return Ok(result);
        }
    }
}

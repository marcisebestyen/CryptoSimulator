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
    [Route("api/trades")]
    public class TradeController : AuthorizedControllerBase
    {
        IUnitOfWork _unitOfWork;
        ITradeService _tradeService;
        IOfferService _offerService;

        public TradeController(IUnitOfWork unitOfWork, ITradeService tradeService, IOfferService offerService) : base(unitOfWork)
        {
            _offerService = offerService;
            _unitOfWork = unitOfWork;
            _tradeService = tradeService;
        }

        private async Task<bool> UserExistsAsync(int userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(new object[] { userId }, null, null);
            return user != null;
        }

        [HttpPost("buy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> BuyCrypto([FromBody] BuyCryptoDto buyDto)
        {
            var denied = await DenyUnlessOwnerAsync(buyDto.UserId);
            if (denied != null)
            {
                return denied;
            }

            if (!await UserExistsAsync(buyDto.UserId))
            {
                return NotFound($"User with ID {buyDto.UserId} not found.");
            }

            var result = await _tradeService.BuyCrypto(buyDto.UserId, buyDto.CryptoId, buyDto.Amount);
            if (result)
            {
                return NoContent();
            }
            return BadRequest("Transaction failed");
        }

        [HttpPost("sell")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SellCrypto([FromBody] SellCryptoDto sellDto)
        {
            var denied = await DenyUnlessOwnerAsync(sellDto.UserId);
            if (denied != null)
            {
                return denied;
            }

            if (!await UserExistsAsync(sellDto.UserId))
            {
                return NotFound($"User with ID {sellDto.UserId} not found.");
            }

            var result = await _tradeService.SellCrypto(sellDto.UserId, sellDto.CryptoId, sellDto.Amount);
            if (result)
            {
                return NoContent();
            }
            return BadRequest("Transaction failed");
        }

        [HttpGet("~/api/users/{userId:int}/portfolio")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PortfolioDto))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PortfolioDto>> GetPortfolio(int userId)
        {
            var denied = await DenyUnlessOwnerOrAdminAsync(userId);
            if (denied != null)
            {
                return denied;
            }

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
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<CryptoProfitLossDto>>> GetDetailedProfitLoss(int userId)
        {
            var denied = await DenyUnlessOwnerOrAdminAsync(userId);
            if (denied != null)
            {
                return denied;
            }

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
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PortfolioProfitLossDto>> GetPortfolioProfitLossSummary(int userId)
        {
            var denied = await DenyUnlessOwnerOrAdminAsync(userId);
            if (denied != null)
            {
                return denied;
            }

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

        private ActionResult ToActionResult(OfferResult result)
        {
            return result.Status switch
            {
                OfferResultStatus.Success => NoContent(),
                OfferResultStatus.Invalid => BadRequest(result.Error),
                OfferResultStatus.NotFound => NotFound(result.Error),
                OfferResultStatus.NotOwner => StatusCode(StatusCodes.Status403Forbidden, result.Error),
                _ => Conflict(result.Error)
            };
        }

        [HttpGet("offers")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<MarketOfferDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IReadOnlyList<MarketOfferDto>>> GetMarketOffers(CancellationToken ct)
        {
            if (CurrentUserId is not int userId)
            {
                return Unauthorized();
            }

            return Ok(await _offerService.GetMarketOffersAsync(userId, ct));
        }

        [HttpGet("offers/mine")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<MyOfferDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IReadOnlyList<MyOfferDto>>> GetMyOffers(CancellationToken ct)
        {
            if (CurrentUserId is not int userId)
            {
                return Unauthorized();
            }

            return Ok(await _offerService.GetMyOffersAsync(userId, ct));
        }

        [HttpPost("offers")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(MyOfferDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MyOfferDto>> CreateOffer([FromBody] CreateOfferDto dto, CancellationToken ct)
        {
            if (CurrentUserId is not int userId)
            {
                return Unauthorized();
            }

            var result = await _offerService.CreateAsync(userId, dto, ct);
            return result.Status switch
            {
                OfferResultStatus.Success => Created("/api/trades/offers/mine", result.Offer),
                OfferResultStatus.NotFound => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }

        [HttpDelete("offers/{offerId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> CancelOffer(int offerId, CancellationToken ct)
        {
            if (CurrentUserId is not int userId)
            {
                return Unauthorized();
            }

            return ToActionResult(await _offerService.CancelAsync(userId, offerId, ct));
        }

        [HttpPost("offers/{offerId:int}/buy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> BuyOffer(int offerId, [FromBody] BuyOfferDto dto, CancellationToken ct)
        {
            if (CurrentUserId is not int userId)
            {
                return Unauthorized();
            }

            return ToActionResult(await _offerService.BuyAsync(userId, offerId, dto.Amount, ct));
        }
    }
}

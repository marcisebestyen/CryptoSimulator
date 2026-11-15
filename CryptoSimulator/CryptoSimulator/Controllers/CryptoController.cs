using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/cryptos")]
    public class CryptoController : ControllerBase
    {
        private const int MaxHistoryPageSize = 100;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICryptoService _cryptoService;

        public CryptoController(IUnitOfWork unitOfWork, IMapper mapper, ICryptoService cryptoService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cryptoService = cryptoService;
        }

        [HttpGet("{cryptoId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CryptoGetDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CryptoGetDto>> GetCrypto(int cryptoId)
        {
            var crypto = await _unitOfWork.CryptoRepository.GetByIdAsync(new object[] { cryptoId }, null, null);
            if (crypto == null)
            {
                return NotFound($"Crypto with ID {cryptoId} not found.");
            }
            return Ok(_mapper.Map<CryptoGetDto>(crypto));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CryptoGetWithCurrentValueDto>))]
        public async Task<ActionResult<IEnumerable<CryptoGetWithCurrentValueDto>>> GetAllCryptos()
        {
            var cryptosWithCurrentValue = await _cryptoService.GetAllWithCurrentValueAsync();
            return Ok(cryptosWithCurrentValue);
        }

        [HttpPut("{cryptoId:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> RenameCrypto(int cryptoId, [FromBody] CryptoPutDto dto)
        {
            var crypto = await _unitOfWork.CryptoRepository.GetByIdAsync(new object[] { cryptoId });
            if (crypto == null)
            {
                return NotFound($"Crypto with ID {cryptoId} not found.");
            }

            if (await _cryptoService.IsNameTakenAsync(dto.Name, cryptoId))
            {
                return Conflict($"A crypto named '{dto.Name}' already exists.");
            }

            crypto.Name = dto.Name;
            await _unitOfWork.CryptoRepository.UpdateAsync(crypto);
            await _unitOfWork.SaveAsync();

            return NoContent();
        }

        [HttpPut("{cryptoId:int}/price")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateCryptoPrice(int cryptoId, [FromBody] ManualPriceUpdateDto dto)
        {
            var cryptoExist = await _unitOfWork.CryptoRepository.GetByIdAsync(new object[] { cryptoId });
            if (cryptoExist == null)
            {
                return NotFound($"Crypto with ID {cryptoId} not found.");
            }

            DateTime updateTimestamp = DateTime.UtcNow;

            bool success = await _cryptoService.UpdateCryptoPriceAsync(cryptoId, dto.NewPrice, updateTimestamp);
            if (!success)
            {
                return BadRequest("Failed to process price update. Check input or logs");
            }

            await _unitOfWork.SaveAsync();

            return NoContent();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CryptoGetDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<CryptoGetDto>> CreateCrypto(CryptoPostDto dto)
        {
            if (await _cryptoService.IsNameTakenAsync(dto.Name))
            {
                return Conflict($"A crypto named '{dto.Name}' already exists.");
            }

            var crypto = _mapper.Map<Crypto>(dto);
            await _cryptoService.AddWithInitialPriceAsync(crypto, dto.InitialPrice, DateTime.UtcNow);
            await _unitOfWork.SaveAsync();
            var cryptoController = _mapper.Map<CryptoGetDto>(crypto);
            return CreatedAtAction(nameof(GetCrypto), new { cryptoId = crypto.Id }, cryptoController);
        }

        [HttpDelete("{cryptoId:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> DeleteCrypto(int cryptoId)
        {
            var crypto = await _unitOfWork.CryptoRepository.GetByIdAsync(new object[] { cryptoId }, null, null);
            if (crypto == null)
            {
                return NotFound($"Crypto with ID {cryptoId} not found.");
            }
            if (await _cryptoService.HasOffersAsync(cryptoId))
            {
                return Conflict("This crypto cannot be deleted because offers reference it.");
            }

            await _unitOfWork.CryptoRepository.DeleteAsync(cryptoId);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }

        [HttpGet("{cryptoId:int}/price-history")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<CryptoPriceHistoryDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedResult<CryptoPriceHistoryDto>>> GetCryptoPriceHistory(int cryptoId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
        {
            if (page < 1 || pageSize < 1)
            {
                return BadRequest("page and pageSize must be at least 1.");
            }

            var cryptoExist = await _unitOfWork.CryptoRepository.GetByIdAsync(new object[] { cryptoId });
            if (cryptoExist == null)
            {
                return NotFound($"Crypto with ID {cryptoId} not found.");
            }

            var history = await _cryptoService.GetCryptoPriceHistoryAsync(cryptoId, page, Math.Min(pageSize, MaxHistoryPageSize));
            return Ok(history);
        }
    }
}

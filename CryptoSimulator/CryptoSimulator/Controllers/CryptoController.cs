using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Route("api/cryptos")]
    public class CryptoController : ControllerBase
    {
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

        [HttpPut("{cryptoId:int}/price")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateCryptoPrice(int cryptoId, [FromBody] ManualPriceUpdateDto dto)
        {
            DateTime updateTimestamp = DateTime.UtcNow;

            bool success = await _cryptoService.UpdateCryptoPriceAsync(cryptoId, dto.NewPrice, updateTimestamp);
            if (!success)
            {
                var cryptoExist = await _unitOfWork.CryptoRepository.GetByIdAsync(new object[] { cryptoId });
                if (cryptoExist == null)
                {
                    return NotFound($"Crypto with ID {cryptoId} not found.");
                }

                return BadRequest("Failed to process price update. Check input or logs");
            }

            await _unitOfWork.SaveAsync();

            return NoContent();
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CryptoGetDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CryptoGetDto>> CreateCrypto(CryptoPostDto dto)
        {
            var crypto = _mapper.Map<Crypto>(dto);
            await _unitOfWork.CryptoRepository.InsertAsync(crypto);
            await _unitOfWork.SaveAsync();
            var cryptoController = _mapper.Map<CryptoGetDto>(crypto);
            return CreatedAtAction(nameof(GetCrypto), new { cryptoId = crypto.Id }, cryptoController);
        }

        [HttpDelete("{cryptoId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteCrypto(int cryptoId)
        {
            var crypto = await _unitOfWork.CryptoRepository.GetByIdAsync(new object[] { cryptoId }, null, null);
            if (crypto == null)
            {
                return NotFound($"Crypto with ID {cryptoId} not found.");
            }
            await _unitOfWork.CryptoRepository.DeleteAsync(cryptoId);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }

        [HttpGet("{cryptoId:int}/price-history")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CryptoPriceHistoryDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<CryptoPriceHistoryDto>>> GetCryptoPriceHistory(int cryptoId)
        {
            var cryptoExist = await _unitOfWork.CryptoRepository.GetByIdAsync(new object[] { cryptoId });
            if (cryptoExist == null)
            {
                return NotFound($"Crypto with ID {cryptoId} not found.");
            }

            var history = await _cryptoService.GetCryptoPriceHistoryAsync(cryptoId);
            return Ok(history);
        }
    }
}

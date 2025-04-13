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
    [Route("[controller]")]
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

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CryptoGetDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CryptoGetDto>> GetCrypto(int id)
        {
            var user = await _unitOfWork.CryptoRepository.GetByIdAsync(new object[] { id }, null, null);
            if (user == null)
            {
                return NotFound($"Crypto with ID {id} not found.");
            }
            return Ok(_mapper.Map<CryptoGetDto>(user));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CryptoGetWithCurrentValueDto>))]
        public async Task<ActionResult<IEnumerable<CryptoGetWithCurrentValueDto>>> GetAllCryptos()
        {
            var cryptosWithCurrentValue = await _cryptoService.GetAllWithCurrentValueAsync();
            return Ok(cryptosWithCurrentValue);
        }

        [HttpPut("price/{cryptoId}")]
        //[Authorize(Roles = "Admin")] // Just in case if we implement real login and authorization.
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateCryptoPriceAsync(int cryptoId, [FromBody] ManualPriceUpdateDto dto)
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
            return CreatedAtAction(nameof(GetCrypto), new { id = crypto.Id }, cryptoController);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteCrypto(int id)
        {
            var crypto = await _unitOfWork.CryptoRepository.GetByIdAsync(new object[] { id }, null, null);
            if (crypto == null)
            {
                return NotFound($"Crypto with ID {id} not found.");
            }
            await _unitOfWork.CryptoRepository.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }
    }
}

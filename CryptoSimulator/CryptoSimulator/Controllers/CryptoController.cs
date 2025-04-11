using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
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

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CryptoGetDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CryptoGetDto>> UpdateCrypto(int id, [FromBody] CryptoPutDto dto)
        {
            var crypto = await _unitOfWork.CryptoRepository.GetByIdAsync(new object[] { id }, null, null);
            if (crypto == null)
            {
                return NotFound($"Crypto with ID {id} not found.");
            }
            _mapper.Map(dto, crypto);
            await _unitOfWork.CryptoRepository.UpdateAsync(crypto);
            await _unitOfWork.SaveAsync();
            return Ok(_mapper.Map<CryptoGetDto>(crypto));
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

        [HttpPost("buy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> BuyCrypto([FromBody] BuyCryptoDto buyDto)
        {
            var result = await _cryptoService.BuyCrypto(buyDto.UserId, buyDto.CryptoId, buyDto.Amount);
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
            var result = await _cryptoService.SellCrypto(sellDto.UserId, sellDto.CryptoId, sellDto.Amount);
            if (result)
            {
                return Ok("Crypto sold successfully");
            }
            return BadRequest("Transaction failed");
        }
    }
}

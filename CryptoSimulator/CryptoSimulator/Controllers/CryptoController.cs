using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CryptoController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CryptoController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CryptoGetDto>> GetCrypto(int id)
        {
            var user = await _unitOfWork.CryptoRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<CryptoGetDto>(user));
        }

        [HttpPost]
        public async Task<ActionResult<CryptoGetDto>> CreateCrypto(CryptoPostDto dto)
        {
            var crypto = _mapper.Map<Crypto>(dto);
            await _unitOfWork.CryptoRepository.InsertAsync(crypto);
            await _unitOfWork.SaveAsync();
            var cryptoController = _mapper.Map<CryptoGetDto>(crypto);
            return CreatedAtAction(nameof(GetCrypto), new { id = crypto.Id }, cryptoController);
        }

        [HttpPut]
        public async Task<ActionResult<CryptoGetDto>> UpdateCrypto(CryptoPutDto dto)
        {
            var crypto = await _unitOfWork.CryptoRepository.GetByIdAsync(dto.Id);
            if (crypto == null)
            {
                return NotFound();
            }
            _mapper.Map(dto, crypto);
            await _unitOfWork.CryptoRepository.UpdateAsync(crypto);
            await _unitOfWork.SaveAsync();
            return Ok(_mapper.Map<CryptoGetDto>(crypto));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCrypto(int id)
        {
            var crypto = await _unitOfWork.CryptoRepository.GetByIdAsync(id);
            if (crypto == null)
            {
                return NotFound();
            }
            await _unitOfWork.CryptoRepository.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }
    }
}

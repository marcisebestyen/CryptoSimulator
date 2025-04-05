using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CryptoLogController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CryptoLogController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CryptoLogDto>> GetCryptoLog(int id)
        {
            var cryptoLog = await _unitOfWork.CryptoLogRepository.GetByIdAsync(id);
            if (cryptoLog == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<CryptoLogDto>(cryptoLog));
        }

        [HttpPost]
        public async Task<ActionResult<CryptoLogDto>> CreateCryptoLog(CryptoLogDto dto)
        {
            var cryptoLog = _mapper.Map<CryptoLog>(dto);
            await _unitOfWork.CryptoLogRepository.InsertAsync(cryptoLog);
            await _unitOfWork.SaveAsync();
            var cryptoControllerDto = _mapper.Map<CryptoLogDto>(cryptoLog);
            return CreatedAtAction(nameof(GetCryptoLog), new { id = cryptoLog.CryptoId }, cryptoControllerDto);
        }

        [HttpPut]
        public async Task<ActionResult<CryptoLogDto>> UpdateCryptoLog(CryptoLogDto dto)
        {
            var cryptoLog = await _unitOfWork.CryptoLogRepository.GetByIdAsync(dto.CryptoId);
            if (cryptoLog == null)
            {
                return NotFound();
            }
            _mapper.Map(dto, cryptoLog);
            await _unitOfWork.CryptoLogRepository.UpdateAsync(cryptoLog);
            await _unitOfWork.SaveAsync();
            return Ok(_mapper.Map<CryptoLogDto>(cryptoLog));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCryptoLog(int id)
        {
            var cryptoLog = await _unitOfWork.CryptoLogRepository.GetByIdAsync(id);
            if (cryptoLog == null)
            {
                return NotFound();
            }
            await _unitOfWork.CryptoLogRepository.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }
    }
}

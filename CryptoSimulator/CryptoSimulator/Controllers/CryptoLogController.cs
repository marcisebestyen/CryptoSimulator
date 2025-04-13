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

        //// Example format for using swagger ==> 2025-04-08T10:00:00Z
        //[HttpGet("{cryptoId}")]
        //[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CryptoLogDto))]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<ActionResult<CryptoLogDto>> GetCryptoLog(int cryptoId, [FromQuery] DateTime from)
        //{
        //    var cryptoLog = await _unitOfWork.CryptoLogRepository.GetByIdAsync(new object[] { cryptoId, from }, null, null);
        //    if (cryptoLog == null)
        //    {
        //        return NotFound($"CryptoLog entry for Crypto ID {cryptoId} and From timestamp {from:o} not found."); // "o" for ISO 8601 format
        //    }
        //    return Ok(_mapper.Map<CryptoLogDto>(cryptoLog));
        //}

        //[HttpPost]
        //[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CryptoLogDto))]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<ActionResult<CryptoLogDto>> CreateCryptoLog([FromBody] CryptoLogDto dto)
        //{
        //    if (dto.CryptoId <= 0 || dto.From == default(DateTime))
        //    {
        //        return BadRequest("CryptoId and From timestamp must be provided and valid.");
        //    }

        //    var cryptoLog = _mapper.Map<CryptoLog>(dto);

        //    var existing = await _unitOfWork.CryptoLogRepository.GetByIdAsync(new object[] { cryptoLog.CryptoId, cryptoLog.From });
        //    if (existing != null)
        //    {
        //        return BadRequest("This exact crypto log entry already exists.");
        //    }

        //    await _unitOfWork.CryptoLogRepository.InsertAsync(cryptoLog);
        //    await _unitOfWork.SaveAsync();
        //    var cryptoControllerDto = _mapper.Map<CryptoLogDto>(cryptoLog);
        //    return CreatedAtAction(nameof(GetCryptoLog), new { cryptoId = cryptoLog.CryptoId, from = cryptoLog.From.ToString("o") }, cryptoControllerDto);
        //}

        //[HttpPut("{cryptoId}")]
        //[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CryptoLogDto))]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<ActionResult<CryptoLogDto>> UpdateCryptoLog(int cryptoId, [FromQuery] DateTime from, CryptoLogDto dto)
        //{
        //    if (cryptoId != dto.CryptoId || from != dto.From)
        //    {
        //        return BadRequest("Route/Query keys do not match payload keys.");
        //    }

        //    var cryptoLog = await _unitOfWork.CryptoLogRepository.GetByIdAsync(new object[] { cryptoId, from }, null, null);
        //    if (cryptoLog == null)
        //    {
        //        return NotFound($"CryptoLog entry for Crypto ID {cryptoId} and From timestamp {from:o} not found.");
        //    }

        //    _mapper.Map(dto, cryptoLog);
        //    await _unitOfWork.CryptoLogRepository.UpdateAsync(cryptoLog);
        //    await _unitOfWork.SaveAsync();
        //    return Ok(_mapper.Map<CryptoLogDto>(cryptoLog));
        //}

        //[HttpDelete("{cryptoId}")]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<ActionResult> DeleteCryptoLog(int cryptoId, [FromQuery] DateTime from)
        //{
        //    var cryptoLog = await _unitOfWork.CryptoLogRepository.GetByIdAsync(new object[] { cryptoId, from }, null, null);
        //    if (cryptoLog == null)
        //    {
        //        return NotFound($"CryptoLog entry for Crypto ID {cryptoId} and From timestamp {from:o} not found.");
        //    }
        //    await _unitOfWork.CryptoLogRepository.DeleteAsync(cryptoId, from);
        //    await _unitOfWork.SaveAsync();
        //    return NoContent();
        //}
    }
}

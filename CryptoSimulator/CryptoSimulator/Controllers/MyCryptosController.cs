using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MyCryptosController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public MyCryptosController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        //[HttpGet("{walletId}/{cryptoId}")]
        //[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MyCryptosDto))]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<ActionResult<MyCryptosDto>> GetMyCrypto(int walletId, int cryptoId)
        //{
        //    var myCrypto = await _unitOfWork.MyCryptosRepository.GetByIdAsync(new object[] { walletId, cryptoId }, null, null);
        //    if (myCrypto == null)
        //    {
        //        return NotFound($"MyCryptos entry for Wallet ID {walletId} and Crypto ID {cryptoId} not found.");
        //    }
        //    return Ok(_mapper.Map<MyCryptosDto>(myCrypto));
        //}

        //[HttpPost]
        //[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(MyCryptosDto))]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<ActionResult<MyCryptosDto>> CreateMyCrypto([FromBody] MyCryptosDto dto)
        //{
        //    if (dto.WalletId <= 0 || dto.CryptoId <= 0)
        //    {
        //        return BadRequest("WalletId and CryptoId must be provided and valid.");
        //    }

        //    var myCrypto = _mapper.Map<MyCryptos>(dto);

        //    var existing = await _unitOfWork.MyCryptosRepository.GetByIdAsync(new object[] { myCrypto.WalletId, myCrypto.CryptoId });
        //    if (existing != null)
        //    {
        //        return BadRequest("This crypto holding already exists for this wallet.");
        //    }

        //    await _unitOfWork.MyCryptosRepository.InsertAsync(myCrypto);
        //    await _unitOfWork.SaveAsync();
        //    var myCryptoDto = _mapper.Map<MyCryptosDto>(myCrypto);
        //    return CreatedAtAction(nameof(GetMyCrypto), new { walletId = myCrypto.WalletId, cryptoId = myCrypto.CryptoId }, myCryptoDto);
        //}

        //[HttpPut("{walletId}/{cryptoId}")]
        //[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MyCryptosDto))]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<ActionResult<MyCryptosDto>> UpdateMyCrypto(int walletId, int cryptoId, [FromBody] MyCryptosDto dto)
        //{
        //    if (walletId != dto.WalletId || cryptoId != dto.CryptoId)
        //    {
        //        return BadRequest("Route keys do not match payload keys.");
        //    }

        //    var myCrypto = await _unitOfWork.MyCryptosRepository.GetByIdAsync(new object[] { walletId, cryptoId }, null, null);
        //    if (myCrypto == null)
        //    {
        //        return NotFound($"MyCryptos entry for Wallet ID {walletId} and Crypto ID {cryptoId} not found.");
        //    }

        //    _mapper.Map(dto, myCrypto);
        //    await _unitOfWork.MyCryptosRepository.UpdateAsync(myCrypto);
        //    await _unitOfWork.SaveAsync();
        //    return Ok(_mapper.Map<MyCryptosDto>(myCrypto));
        //}

        //[HttpDelete("{walletId}/{cryptoId}")]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<ActionResult> DeleteMyCrypto(int walletId, int cryptoId)
        //{
        //    var myCrypto = await _unitOfWork.MyCryptosRepository.GetByIdAsync(new object[] { walletId, cryptoId }, null, null);
        //    if (myCrypto == null)
        //    {
        //        return NotFound($"MyCryptos entry for Wallet ID {walletId} and Crypto ID {cryptoId} not found.");
        //    }
        //    await _unitOfWork.MyCryptosRepository.DeleteAsync(walletId, cryptoId);
        //    await _unitOfWork.SaveAsync();
        //    return NoContent();
        //}
    }
}

using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WalletController : ControllerBase
    { 
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public WalletController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WalletGetDto>> GetWallet(int id)
        {
            var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(id);
            if (wallet == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<WalletGetDto>(wallet));
        }

        [HttpPost]
        public async Task<ActionResult<WalletGetDto>> CreateWallet(WalletPostDto dto)
        {
            var wallet = _mapper.Map<Wallet>(dto);
            await _unitOfWork.WalletRepository.InsertAsync(wallet);
            await _unitOfWork.SaveAsync();
            var walletDto = _mapper.Map<WalletGetDto>(wallet);
            return CreatedAtAction(nameof(GetWallet), new { id = walletDto.Id }, walletDto);
        }

        [HttpPut]
        public async Task<ActionResult<WalletGetDto>> UpdateWallet(WalletPutDto dto)
        {
            var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(dto.Id);
            if (wallet == null)
            {
                return NotFound();
            }
            _mapper.Map(dto, wallet);
            await _unitOfWork.WalletRepository.UpdateAsync(wallet);
            await _unitOfWork.SaveAsync();
            return Ok(_mapper.Map<WalletGetDto>(wallet));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteWallet(int id)
        {
            var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(id);
            if (wallet == null)
            {
                return NotFound();
            }
            await _unitOfWork.WalletRepository.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }
    }
}

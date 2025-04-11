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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WalletGetDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WalletGetDto>> GetWallet(int id)
        {
            var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(new object[] { id }, null, null);
            if (wallet == null)
            {
                return NotFound($"Wallet with ID {id} not found.");
            }
            return Ok(_mapper.Map<WalletGetDto>(wallet));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WalletGetDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<WalletGetDto>> CreateWallet(WalletPostDto dto)
        {
            var wallet = _mapper.Map<Wallet>(dto);
            await _unitOfWork.WalletRepository.InsertAsync(wallet);
            await _unitOfWork.SaveAsync();
            var walletDto = _mapper.Map<WalletGetDto>(wallet);
            return CreatedAtAction(nameof(GetWallet), new { id = walletDto.Id }, walletDto);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WalletGetDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WalletGetDto>> UpdateWallet(int id, [FromBody] WalletPutDto dto)
        {
            var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(new object[] { id }, null, null);
            if (wallet == null)
            {
                return NotFound($"Wallet with ID {id} not found.");
            }
            _mapper.Map(dto, wallet);
            await _unitOfWork.WalletRepository.UpdateAsync(wallet);
            await _unitOfWork.SaveAsync();
            return Ok(_mapper.Map<WalletGetDto>(wallet));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteWallet(int id)
        {
            var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(new object[] { id }, null, null);
            if (wallet == null)
            {
                return NotFound($"Wallet with ID {id} not found.");
            }
            await _unitOfWork.WalletRepository.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }
    }
}

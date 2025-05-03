using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Route("api/wallet")]
    public class WalletController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IWalletService _walletService;

        public WalletController(IUnitOfWork unitOfWork, IMapper mapper, IWalletService walletService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _walletService = walletService;
        }

        [HttpGet("{id}")] 
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserWalletDetailsDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserWalletDetailsDto>> GetUserWallet(int id) 
        {
            var walletDetails = await _walletService.GetUserWalletDetailsAsync(id);

            if (walletDetails == null)
            {
                return NotFound($"Wallet details for User ID {id} not found.");
            }

            return Ok(walletDetails);
        }

        //[HttpGet("{id}")]
        //[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WalletGetDto))]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<ActionResult<WalletGetDto>> GetWallet(int id)
        //{
        //    var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(new object[] { id }, null, null);
        //    if (wallet == null)
        //    {
        //        return NotFound($"Wallet with ID {id} not found.");
        //    }
        //    return Ok(_mapper.Map<WalletGetDto>(wallet));
        //}

        //[HttpPost]
        //[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WalletGetDto))]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<ActionResult<WalletGetDto>> CreateTransaction(WalletGetDto dto)
        //{
        //    var wallet = _mapper.Map<Wallet>(dto);
        //    await _unitOfWork.WalletRepository.InsertAsync(wallet);
        //    await _unitOfWork.SaveAsync();
        //    var walletDto = _mapper.Map<WalletGetDto>(wallet);
        //    return CreatedAtAction(nameof(GetWallet), new { id = wallet.Id }, walletDto);
        //}

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

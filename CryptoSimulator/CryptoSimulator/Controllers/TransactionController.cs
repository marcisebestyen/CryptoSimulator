using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/transactions")]
    public class TransactionController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITransactionService _transactionService;

        public TransactionController(IUnitOfWork unitOfWork, IMapper mapper, ITransactionService transactionsService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _transactionService = transactionsService;
        }

        [HttpGet("{transactionId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TransactionsGetDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TransactionsGetDto>> GetTransaction(int transactionId)
        {
            var transactions = await _unitOfWork.TransactionRepository.GetAsync(t => t.Id == transactionId, new[] { "Crypto", "Wallet.User" });
            var transaction = transactions.FirstOrDefault();
            if (transaction == null)
            {
                return NotFound($"Transaction with ID {transactionId} not found.");
            }
            return Ok(_mapper.Map<TransactionsGetDto>(transaction));
        }

        [HttpGet("~/api/users/{userId:int}/transactions")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TransactionsGetDto>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<TransactionsGetDto>>> GetUserTransactions(int userId, bool newestFirst = true)
        {
            var transactions = await _transactionService.GetUserTransactionsAsync(userId, newestFirst);
            if (transactions == null)
            {
                return NotFound($"User or Wallet not found for User ID {userId}.");
            }
            return Ok(transactions);
        }
    }
}

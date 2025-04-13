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

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TransactionsGetDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TransactionsGetDto>> GetTransaction(int id)
        {
            var transactions = await _unitOfWork.TransactionRepository.GetAsync(t => t.Id == id, new[] { "Crypto", "Wallet.User" });
            var transaction = transactions.FirstOrDefault();
            if (transaction == null)
            {
                return NotFound($"Transaction with ID {id} not found.");
            }
            return Ok(_mapper.Map<TransactionsGetDto>(transaction));
        }

        [HttpGet("user/{userId}")]
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

        //[HttpPost]
        //[ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TransactionsGetDto))]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<ActionResult<TransactionsGetDto>> CreateTransaction(TransactionsPostDto dto)
        //{
        //    var transaction = _mapper.Map<Transactions>(dto);
        //    await _unitOfWork.TransactionRepository.InsertAsync(transaction);
        //    await _unitOfWork.SaveAsync();
        //    var transactionDto = _mapper.Map<TransactionsGetDto>(transaction);
        //    return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transactionDto);
        //}

        //[HttpPut("{id}")]
        //[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TransactionsGetDto))]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<ActionResult<TransactionsGetDto>> UpdateTransaction(int id, [FromBody] TransactionsPutDto dto)
        //{
        //    var transaction = await _unitOfWork.TransactionRepository.GetByIdAsync(new object[] { id }, null, null);
        //    if (transaction == null)
        //    {
        //        return NotFound($"Transaction with ID {id} not found.");
        //    }
        //    _mapper.Map(dto, transaction);
        //    await _unitOfWork.TransactionRepository.UpdateAsync(transaction);
        //    await _unitOfWork.SaveAsync();
        //    return Ok(_mapper.Map<TransactionsGetDto>(transaction));
        //}

        //[HttpDelete("{id}")]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<ActionResult> DeleteTransaction(int id)
        //{
        //    var transaction = await _unitOfWork.TransactionRepository.GetByIdAsync(new object[] { id }, null, null);
        //    if (transaction == null)
        //    {
        //        return NotFound($"Transaction with ID {id} not found.");
        //    }
        //    await _unitOfWork.TransactionRepository.DeleteAsync(id);
        //    await _unitOfWork.SaveAsync();
        //    return NoContent();
        //}
    }
}

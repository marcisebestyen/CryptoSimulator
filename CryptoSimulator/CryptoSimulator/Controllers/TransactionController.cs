using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TransactionController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TransactionsGetDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TransactionsGetDto>> GetTransaction(int id)
        {
            var transaction = await _unitOfWork.TransactionRepository.GetByIdAsync(new object[] { id }, null, null);
            if (transaction == null)
            {
                return NotFound($"Transaction with ID {id} not found.");
            }
            return Ok(_mapper.Map<TransactionsGetDto>(transaction));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TransactionsGetDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TransactionsGetDto>> CreateTransaction(TransactionsPostDto dto)
        {
            var transaction = _mapper.Map<Transactions>(dto);
            await _unitOfWork.TransactionRepository.InsertAsync(transaction);
            await _unitOfWork.SaveAsync();
            var transactionDto = _mapper.Map<TransactionsGetDto>(transaction);
            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transactionDto);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TransactionsGetDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TransactionsGetDto>> UpdateTransaction(int id, [FromBody] TransactionsPutDto dto)
        {
            var transaction = await _unitOfWork.TransactionRepository.GetByIdAsync(new object[] { id }, null, null);
            if (transaction == null)
            {
                return NotFound($"Transaction with ID {id} not found.");
            }
            _mapper.Map(dto, transaction);
            await _unitOfWork.TransactionRepository.UpdateAsync(transaction);
            await _unitOfWork.SaveAsync();
            return Ok(_mapper.Map<TransactionsGetDto>(transaction));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteTransaction(int id)
        {
            var transaction = await _unitOfWork.TransactionRepository.GetByIdAsync(new object[] { id }, null, null);
            if (transaction == null)
            {
                return NotFound($"Transaction with ID {id} not found.");
            }
            await _unitOfWork.TransactionRepository.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }
    }
}

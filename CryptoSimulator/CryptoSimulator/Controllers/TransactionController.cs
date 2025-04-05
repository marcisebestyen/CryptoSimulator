using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Transactions;

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
        public async Task<ActionResult<TransactionsGetDto>> GetTransaction(int id)
        {
            var transaction = await _unitOfWork.TransactionRepository.GetByIdAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<TransactionsGetDto>(transaction));
        }

        [HttpPost]
        public async Task<ActionResult<TransactionsGetDto>> CreateTransaction(TransactionsPostDto dto)
        {
            var transaction = _mapper.Map<Transactions>(dto);
            await _unitOfWork.TransactionRepository.InsertAsync(transaction);
            await _unitOfWork.SaveAsync();
            var transactionDto = _mapper.Map<TransactionsGetDto>(transaction);
            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transactionDto);
        }

        [HttpPut]
        public async Task<ActionResult<TransactionsGetDto>> UpdateTransaction(TransactionsPutDto dto)
        {
            var transaction = await _unitOfWork.TransactionRepository.GetByIdAsync(dto.Id);
            if (transaction == null)
            {
                return NotFound();
            }
            _mapper.Map(dto, transaction);
            await _unitOfWork.TransactionRepository.UpdateAsync(transaction);
            await _unitOfWork.SaveAsync();
            return Ok(_mapper.Map<TransactionsGetDto>(transaction));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTransaction(int id)
        {
            var transaction = await _unitOfWork.TransactionRepository.GetByIdAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }
            await _unitOfWork.TransactionRepository.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }
    }
}

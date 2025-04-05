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

        [HttpGet("{id}")]
        public async Task<ActionResult<MyCryptosDto>> GetMyCrypto(int id)
        {
            var myCrypto = await _unitOfWork.MyCryptosRepository.GetByIdAsync(id);
            if (myCrypto == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<MyCryptosDto>(myCrypto));
        }

        [HttpPost]
        public async Task<ActionResult<MyCryptosDto>> CreateMyCrypto(MyCryptosDto dto)
        {
            var myCrypto = _mapper.Map<MyCryptos>(dto);
            await _unitOfWork.MyCryptosRepository.InsertAsync(myCrypto);
            await _unitOfWork.SaveAsync();
            var myCryptoDto = _mapper.Map<MyCryptosDto>(myCrypto);
            return CreatedAtAction(nameof(GetMyCrypto), new { id = myCrypto.CryptoId }, myCryptoDto);
        }

        [HttpPut]
        public async Task<ActionResult<MyCryptosDto>> UpdateMyCrypto(MyCryptosDto dto)
        {
            var myCrypto = await _unitOfWork.MyCryptosRepository.GetByIdAsync(dto.CryptoId);
            if (myCrypto == null)
            {
                return NotFound();
            }
            _mapper.Map(dto, myCrypto);
            await _unitOfWork.MyCryptosRepository.UpdateAsync(myCrypto);
            await _unitOfWork.SaveAsync();
            return Ok(_mapper.Map<MyCryptosDto>(myCrypto));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMyCrypto(int id)
        {
            var myCrypto = await _unitOfWork.MyCryptosRepository.GetByIdAsync(id);
            if (myCrypto == null)
            {
                return NotFound();
            }
            await _unitOfWork.MyCryptosRepository.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }
    }
}

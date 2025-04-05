using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Repositories;
using Microsoft.AspNetCore.Mvc;
using CryptoSimulator.Entities;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UserController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserGetDto>> GetUser(int id)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<UserGetDto>(user));
        }

        [HttpPost]
        public async Task<ActionResult<UserGetDto>> CreateUser(UserPostDto dto)
        {
            var user = _mapper.Map<User>(dto);
            await _unitOfWork.UserRepository.InsertAsync(user);
            await _unitOfWork.SaveAsync();
            var userGetDto = _mapper.Map<UserGetDto>(user);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userGetDto);
        }

        [HttpPut]
        public async Task<ActionResult<UserGetDto>> UpdateUser(UserPutDto dto)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(dto.Id);
            if (user == null)
            {
                return NotFound();
            }
            _mapper.Map(dto, user);
            await _unitOfWork.UserRepository.UpdateAsync(user);
            await _unitOfWork.SaveAsync();
            return Ok(_mapper.Map<UserGetDto>(user));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            await _unitOfWork.UserRepository.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }
    }
}

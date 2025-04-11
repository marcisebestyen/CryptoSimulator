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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserGetDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserGetDto>> GetUser(int id)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(new object[] { id }, null, null);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }
            return Ok(_mapper.Map<UserGetDto>(user));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserGetDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserGetDto>> CreateUser(UserPostDto dto)
        {
            var user = _mapper.Map<User>(dto);
            await _unitOfWork.UserRepository.InsertAsync(user);
            await _unitOfWork.SaveAsync();
            var userGetDto = _mapper.Map<UserGetDto>(user);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userGetDto);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserGetDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserGetDto>> UpdateUser(int id, UserPutDto dto)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(new object[] { id }, null, null);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }
            _mapper.Map(dto, user);
            await _unitOfWork.UserRepository.UpdateAsync(user);
            await _unitOfWork.SaveAsync();
            return Ok(_mapper.Map<UserGetDto>(user));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(new object[] { id }, null, null);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }
            await _unitOfWork.UserRepository.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }
    }
}

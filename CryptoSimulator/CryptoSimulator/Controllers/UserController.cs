using AutoMapper;
using Azure.Core;
using CryptoSimulator.DTOs;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;

        public UserController(IUnitOfWork unitOfWork, IMapper mapper, IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userService = userService;
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

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserGetDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserGetDto>> Login([FromBody] UserLoginDto loginDto)
        {
            var loginSuccessful = await _userService.LoginAsync(loginDto.Email, loginDto.Password);
            if (!loginSuccessful)
            {
                return Unauthorized("Invalid email or password.");
            }

            var user = (await _unitOfWork.UserRepository.GetAsync(u => u.Email == loginDto.Email)).FirstOrDefault();
            if (user == null)
            {
                return Unauthorized("An error occurred after login");
            }

            return Ok(_mapper.Map<UserGetDto>(user));
        }

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserGetDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserGetDto>> Register([FromBody] UserRegisterDto registerDto)
        {
            var result = await _userService.RegisterAsync(registerDto.Username, registerDto.Email, registerDto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { Errors = result.Errors });
            }

            try
            {
                await _unitOfWork.SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred during the registration.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred during the registration.");
            }

            if (result.CreatedUser == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Registration completed but user data could not be retrieved.");
            }
            var userGetDto = _mapper.Map<UserGetDto>(result.CreatedUser);

            return CreatedAtAction(nameof(GetUser), new { id = userGetDto.Id }, userGetDto);
        }

        [HttpPost("change-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)] 
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ChangePassword(int userId, [FromBody] UserChangePasswordDto changePasswordDto)
        {
            var changeResult = await _userService.ChangePasswordAsync(userId, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            if (!changeResult.Succeeded)
            {
                foreach (var error in changeResult.Errors)
                {
                    if (error.Contains("User not found", StringComparison.OrdinalIgnoreCase))
                    {
                        return NotFound(error);
                    }
                    ModelState.AddModelError(string.Empty, error);
                }
                return BadRequest(ModelState);
            }
            return NoContent();
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

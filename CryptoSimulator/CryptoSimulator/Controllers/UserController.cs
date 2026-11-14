using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CryptoSimulator.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/users")]
    public class UserController : AuthorizedControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;

        public UserController(IUnitOfWork unitOfWork, IMapper mapper, IUserService userService, ITokenService tokenService) : base(unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userService = userService;
            _tokenService = tokenService;
        }

        [HttpGet("{userId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserGetDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserGetDto>> GetUser(int userId)
        {
            var denied = await DenyUnlessOwnerOrAdminAsync(userId);
            if (denied != null)
            {
                return denied;
            }

            var user = await _unitOfWork.UserRepository.GetByIdAsync(new object[] { userId }, null, null);
            if (user == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }
            return Ok(_mapper.Map<UserGetDto>(user));
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] UserLoginDto loginDto)
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

            return Ok(new LoginResponseDto
            {
                Token = _tokenService.CreateToken(user),
                User = _mapper.Map<UserGetDto>(user),
            });
        }

        [AllowAnonymous]
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
            catch (DbUpdateException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred during the registration.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred during the registration.");
            }

            if (result.CreatedUser == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Registration completed but user data could not be retrieved.");
            }
            var userGetDto = _mapper.Map<UserGetDto>(result.CreatedUser);

            return CreatedAtAction(nameof(GetUser), new { userId = userGetDto.Id }, userGetDto);
        }

        [HttpPost("{userId:int}/change-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> ChangePassword(int userId, [FromBody] UserChangePasswordDto changePasswordDto)
        {
            var denied = await DenyUnlessOwnerAsync(userId);
            if (denied != null)
            {
                return denied;
            }

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

        [HttpPut("{userId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserGetDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserGetDto>> UpdateUser(int userId, UserPutDto dto)
        {
            var denied = await DenyUnlessOwnerOrAdminAsync(userId);
            if (denied != null)
            {
                return denied;
            }

            var user = await _unitOfWork.UserRepository.GetByIdAsync(new object[] { userId }, null, null);
            if (user == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }
            _mapper.Map(dto, user);
            await _unitOfWork.UserRepository.UpdateAsync(user);
            await _unitOfWork.SaveAsync();
            return Ok(_mapper.Map<UserGetDto>(user));
        }

        [HttpDelete("{userId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteUser(int userId)
        {
            var denied = await DenyUnlessOwnerOrAdminAsync(userId);
            if (denied != null)
            {
                return denied;
            }

            var user = await _unitOfWork.UserRepository.GetByIdAsync(new object[] { userId }, null, null);
            if (user == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }
            await _unitOfWork.UserRepository.DeleteAsync(userId);
            await _unitOfWork.SaveAsync();
            return NoContent();
        }
    }
}

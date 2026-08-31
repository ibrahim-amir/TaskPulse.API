using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserServices _userService;

        public AuthController(IUserServices userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(CreateUserDto dto)
        {
            var result = await _userService.CreateUserAsync(dto);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _userService.LoginAsync(dto);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}

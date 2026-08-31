using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.Controllers.Infrastructure;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : BaseApiController
    {
        private readonly IUserServices _userService;

        public AuthController(IUserServices userService)
        {
            _userService = userService;
        }

        // Admin-only. Single-org app, one hardcoded Admin creates every account.
        [HttpPost("register")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> Register(CreateUserDto dto)
        {
            return Respond(await _userService.CreateUserAsync(dto));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            return Respond(await _userService.LoginAsync(dto));
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequestDto dto)
        {
            return Respond(await _userService.RefreshTokenAsync(dto));
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            return Respond(await _userService.LogoutAsync(User.GetUserId()));
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            return Respond(await _userService.GetUserById(User.GetUserId()));
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailDto dto)
        {
            return Respond(await _userService.ConfirmEmailAsync(dto));
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation(ResendConfirmationDto dto)
        {
            return Respond(await _userService.ResendConfirmationAsync(dto));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            return Respond(await _userService.ForgotPasswordAsync(dto));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            return Respond(await _userService.ResetPasswordAsync(dto));
        }
    }
}

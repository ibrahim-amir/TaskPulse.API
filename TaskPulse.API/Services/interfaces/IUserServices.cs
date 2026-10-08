using Microsoft.AspNetCore.Http;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.UserDto;

namespace TaskPulse.API.Services.interfaces
{
    public interface IUserServices
    {
        public Task<CommonResponse<UserDTO>> CreateUserAsync(CreateUserDto dto);

        public Task<CommonResponse<UserDTO>> GetUserById(int id);

        public Task<CommonResponse<PagedResult<UserDTO>>> GetUsersAsync(UserQueryDto query);

        public Task<CommonResponse<UserDTO>> UpdateUserProfileAsync(UpdateUserProfileDto dto);

        // Self-or-admin, same as UpdateUserProfileAsync — checked by the controller.
        public Task<CommonResponse<UserDTO>> UpdateAvatarAsync(int userId, IFormFile file);

        public Task<CommonResponse<UserDTO>> UpdateUserRoleAsync(UpdateUserRoleDto dto);

        public Task<CommonResponse<UserDTO>> SetUserStatusAsync(UpdateUserStatusDto dto);

        // A real row delete — only succeeds when the account has no history
        // that would either block it (SQL Restrict) or disappear with it
        // (comments, uploads, approvals). Use SetUserStatusAsync to disable
        // an account with real activity instead.
        public Task<CommonResponse<string>> DeleteUserAsync(int id, int callerId);

        public Task<CommonResponse<AuthResponseDto>> LoginAsync(LoginDto dto);

        public Task<CommonResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto);

        public Task<CommonResponse<string>> LogoutAsync(int userId);

        public Task<CommonResponse<string>> ConfirmEmailAsync(ConfirmEmailDto dto);

        public Task<CommonResponse<string>> ResendConfirmationAsync(ResendConfirmationDto dto);

        public Task<CommonResponse<string>> ForgotPasswordAsync(ForgotPasswordDto dto);

        public Task<CommonResponse<string>> ResetPasswordAsync(ResetPasswordDto dto);

        public Task<CommonResponse<string>> ChangePasswordAsync(ChangePasswordDto dto);
    }
}

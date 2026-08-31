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

        public Task<CommonResponse<UserDTO>> UpdateUserRoleAsync(UpdateUserRoleDto dto);

        public Task<CommonResponse<UserDTO>> SetUserStatusAsync(UpdateUserStatusDto dto);

        public Task<CommonResponse<AuthResponseDto>> LoginAsync(LoginDto dto);

        public Task<CommonResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto);

        public Task<CommonResponse<string>> LogoutAsync(int userId);

        public Task<CommonResponse<string>> ConfirmEmailAsync(ConfirmEmailDto dto);

        public Task<CommonResponse<string>> ResendConfirmationAsync(ResendConfirmationDto dto);

        public Task<CommonResponse<string>> ForgotPasswordAsync(ForgotPasswordDto dto);

        public Task<CommonResponse<string>> ResetPasswordAsync(ResetPasswordDto dto);
    }
}

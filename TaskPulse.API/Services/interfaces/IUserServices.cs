using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;

namespace TaskPulse.API.Services.interfaces
{
    public interface IUserServices
    {
        public Task<CommonResponse<UserDTO>> CreateUserAsync(CreateUserDto dto);

        public Task<CommonResponse<UserDTO>> GetUserById(int id);

        public Task<CommonResponse<string>> LoginAsync(LoginDto dto);
    }
}

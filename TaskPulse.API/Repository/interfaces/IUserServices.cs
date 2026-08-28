using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities;

namespace TaskPulse.API.Repository.interfaces
{
    public interface IUserServices
    {
        public Task<CommonResponse<UserDTO>> CreateUserAsync(CreateUserDto dto);

        public Task<CommonResponse<UserDTO>> GetUserById(int id);
    }
}

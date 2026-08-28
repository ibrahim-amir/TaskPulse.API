using TaskPulse.API.DTO.UserDto.BaseUserClass;
using TaskPulse.API.Entities;

namespace TaskPulse.API.DTO.UserDto
{
    public class UserDTO : BasicUserDto
    {
        public int Id { get; set; }
        public string? Role { get; set; }
        public string? profileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDisabled { get; set; }

        public static UserDTO CreateUserDtoFromUserEntity(User user)
        {
            return new UserDTO
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(),
                profileImageUrl = user.profileImageUrl,
                CreatedAt = user.CreatedAt,
                IsDisabled = user.IsDisabled
            };
        }
    }
}

using TaskPulse.API.DTO.UserDto.BaseUserClass;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.UserDto
{
    public class UserDTO : BasicUserDto
    {
        public int Id { get; set; }
        public string? profileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDisabled { get; set; }
        public UserRole Role { get; set; }

        public static UserDTO CreateUserDtoFromUserEntity(User user)
        {
            return new UserDTO
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                profileImageUrl = user.profileImageUrl,
                CreatedAt = user.CreatedAt,
                IsDisabled = user.IsDisabled,
                Role = user.Role
            };
        }
    }
}

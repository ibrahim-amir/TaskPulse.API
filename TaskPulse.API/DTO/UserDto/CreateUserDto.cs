using TaskPulse.API.DTO.UserDto.BaseUserClass;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.UserDto;

public class CreateUserDto : BasicUserDto
{
    public UserRole Role { get; set; }
    public string? profileImageUrl { get; set; }
}

using TaskPulse.API.DTO.UserDto.BaseUserClass;

namespace TaskPulse.API.DTO.UserDto;

public class CreateUserDto : BasicUserDto
{
    public string? profileImageUrl { get; set; }
    public string confirmPassword { get; set; }
}

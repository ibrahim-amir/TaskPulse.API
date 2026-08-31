using TaskPulse.API.DTO.UserDto.BaseUserClass;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.UserDto;

public class CreateUserDto : BasicUserDto
{
    public string Password { get; set; } = string.Empty;
    public string? profileImageUrl { get; set; }
    public string confirmPassword { get; set; }

    // Set by the Admin creating the account. Defaults to Member.
    public UserRole Role { get; set; } = UserRole.Member;
}

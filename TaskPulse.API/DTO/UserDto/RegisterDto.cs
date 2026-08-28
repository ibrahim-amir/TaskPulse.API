using TaskPulse.API.DTO.UserDto.BaseUserClass;

namespace TaskPulse.API.DTO.UserDto;

// Request body for POST /api/auth/register.
// Only what a human fills into the register form — no Id, no Role,
// no CreatedAt. The server decides those.
public class RegisterDto : BasicUserDto {}

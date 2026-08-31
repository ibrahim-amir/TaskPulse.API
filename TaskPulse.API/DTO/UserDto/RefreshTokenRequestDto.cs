namespace TaskPulse.API.DTO.UserDto;

// Request body for POST /api/auth/refresh.
public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

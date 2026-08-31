namespace TaskPulse.API.DTO.UserDto;

// Response body for POST /api/auth/login and POST /api/auth/refresh.
public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
}

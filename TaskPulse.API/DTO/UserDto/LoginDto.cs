namespace TaskPulse.API.DTO.UserDto;

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Longer-lived refresh token when true (see Jwt:RememberMeRefreshTokenExpiryDays),
    // short-lived when false (Jwt:RefreshTokenExpiryDays).
    public bool RememberMe { get; set; } = false;
}

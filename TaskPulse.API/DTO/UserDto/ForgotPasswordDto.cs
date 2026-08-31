namespace TaskPulse.API.DTO.UserDto;

// Request body for POST /api/auth/forgot-password.
public class ForgotPasswordDto
{
    public string Email { get; set; } = string.Empty;
}

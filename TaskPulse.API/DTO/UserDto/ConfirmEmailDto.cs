namespace TaskPulse.API.DTO.UserDto;

// Request body for POST /api/auth/confirm-email.
public class ConfirmEmailDto
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

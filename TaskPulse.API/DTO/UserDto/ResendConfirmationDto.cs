namespace TaskPulse.API.DTO.UserDto;

// Request body for POST /api/auth/resend-confirmation.
public class ResendConfirmationDto
{
    public string Email { get; set; } = string.Empty;
}

namespace TaskPulse.API.DTO.UserDto;

// Request body for POST /api/auth/change-password. Also how MustChangePassword
// gets cleared — an Admin-provisioned account calls this once with its
// temporary password as CurrentPassword.
public class ChangePasswordDto
{
    // Filled from the token by the controller, not sent in the body.
    public int Id { get; set; }

    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

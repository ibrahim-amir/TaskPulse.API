namespace TaskPulse.API.DTO.UserDto;

// PUT /api/users/{id}. Profile fields only — role and disabled status have
// their own Admin-only endpoints, so they can't be changed through here.
public class UpdateUserProfileDto
{
    // Filled from the route by the controller, not sent in the body. The
    // validator needs it to check the new name is unique against everyone
    // except this user.
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? profileImageUrl { get; set; }

    // Self-service — whether a Manager can assign this user to a task
    // without needing anyone's approval first.
    public bool IsAvailableForAssignment { get; set; } = true;
}

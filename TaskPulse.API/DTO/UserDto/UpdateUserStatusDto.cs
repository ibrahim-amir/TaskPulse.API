namespace TaskPulse.API.DTO.UserDto;

// PUT /api/users/{id}/status. Admin only. DELETE /api/users/{id} runs through
// the same service method with IsDisabled = true — accounts are disabled, never
// row-deleted, so their tasks, comments and activity history stay intact.
public class UpdateUserStatusDto
{
    // Filled from the route by the controller, not sent in the body.
    public int Id { get; set; }

    public bool IsDisabled { get; set; }
}

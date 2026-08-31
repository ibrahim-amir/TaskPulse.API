using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.UserDto;

// PUT /api/users/{id}/role. Admin only.
public class UpdateUserRoleDto
{
    // Filled from the route by the controller, not sent in the body.
    public int Id { get; set; }

    public UserRole Role { get; set; }
}

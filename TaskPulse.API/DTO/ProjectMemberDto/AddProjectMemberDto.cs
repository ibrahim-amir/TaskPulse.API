using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.ProjectMemberDto;

public class AddProjectMemberDto
{
    // Filled from the route by the controller, not sent in the body.
    public int ProjectId { get; set; }

    public int UserId { get; set; }

    // Their role on this project. Defaults to Member; a Manager added here is
    // a second manager on the project, not a system-wide promotion.
    public UserRole Role { get; set; } = UserRole.Member;
}

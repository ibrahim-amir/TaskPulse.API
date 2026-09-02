using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.ProjectDto;

// Also the payload stored on an ApprovalRequest when a Manager edits a project
// the Admin created — it's serialised whole and replayed on approval.
public class UpdateProjectDto
{
    // Filled from the route by the controller, not sent in the body.
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; }
    public PriorityLevel Priority { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

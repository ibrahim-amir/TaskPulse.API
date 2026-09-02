using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.ProjectDto;

public class CreateProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Who runs the project. Optional for a Manager creating their own — they
    // become the manager. Required when the Admin creates one, since the Admin
    // doesn't run projects themselves.
    public int? ManagerUserId { get; set; }
}

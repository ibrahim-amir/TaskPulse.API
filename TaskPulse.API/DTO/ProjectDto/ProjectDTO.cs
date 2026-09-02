using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.ProjectDto;

public class ProjectDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; }
    public PriorityLevel Priority { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public UserRole CreatedByRole { get; set; }
    public int MemberCount { get; set; }

    public static ProjectDTO FromEntity(Project project)
    {
        return new ProjectDTO
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status,
            Priority = project.Priority,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            CreatedAt = project.CreatedAt,
            CreatedByUserId = project.CreatedByUserId,
            CreatedByRole = project.CreatedByRole,
            MemberCount = project.Members?.Count ?? 0
        };
    }
}

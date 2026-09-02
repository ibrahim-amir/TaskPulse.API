using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.ProjectMemberDto;

public class ProjectMemberDTO
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserRole AssignedByRole { get; set; }
    public DateTime JoinedDate { get; set; }

    // Expects the User navigation to be loaded; the repository includes it.
    public static ProjectMemberDTO FromEntity(ProjectMember member)
    {
        return new ProjectMemberDTO
        {
            Id = member.Id,
            ProjectId = member.ProjectId,
            UserId = member.UserId,
            UserName = member.User?.Name ?? string.Empty,
            UserEmail = member.User?.Email ?? string.Empty,
            Role = member.Role,
            AssignedByRole = member.AssignedByRole,
            JoinedDate = member.CreatedAt
        };
    }
}

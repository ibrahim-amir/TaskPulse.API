using TaskPulse.API.Entities;

namespace TaskPulse.API.DTO.ActivityLogDto;

public class ActivityLogDTO
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
    public DateTime CreatedAt { get; set; }

    public static ActivityLogDTO FromEntity(ActivityLog log)
    {
        return new ActivityLogDTO
        {
            Id = log.Id,
            UserId = log.UserId,
            UserName = log.User?.Name ?? string.Empty,
            Action = log.Action,
            Details = log.Details,
            ProjectId = log.ProjectId,
            TaskId = log.TaskId,
            CreatedAt = log.CreatedAt
        };
    }
}

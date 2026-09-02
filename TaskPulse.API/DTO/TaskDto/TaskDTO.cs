using TaskPulse.API.DTO.SubtaskDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.TaskDto;

public class TaskDTO
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; }
    public PriorityLevel Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }

    public List<int> AssignedUserIds { get; set; } = new();
    public List<SubtaskDTO> Subtasks { get; set; } = new();

    // What the progress bar reads. Computed, never stored — the subtask rows
    // are the single source of truth.
    public int RequiredFileCount { get; set; }
    public int UploadedFileCount { get; set; }
    public int ProgressPercent { get; set; }

    // True once the due date has passed with the task still open.
    public bool IsOverdue { get; set; }

    public static TaskDTO FromEntity(TaskItem task)
    {
        var subtasks = task.Subtasks?.ToList() ?? new List<TaskSubtask>();
        var required = subtasks.Count;
        var uploaded = subtasks.Count(s => s.IsUploaded);

        return new TaskDTO
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            CompletedDate = task.CompletedDate,
            EstimatedHours = task.EstimatedHours,
            ActualHours = task.ActualHours,
            CreatedAt = task.CreatedAt,
            CreatedByUserId = task.CreatedByUserId,
            AssignedUserIds = task.Assignments?.Select(a => a.UserId).ToList() ?? new List<int>(),
            Subtasks = subtasks.Select(SubtaskDTO.FromEntity).ToList(),
            RequiredFileCount = required,
            UploadedFileCount = uploaded,
            ProgressPercent = required == 0 ? 0 : (int)Math.Round(uploaded * 100.0 / required),
            IsOverdue = task.DueDate.HasValue
                        && task.DueDate.Value < DateTime.UtcNow
                        && task.Status != TaskItemStatus.Done
        };
    }
}

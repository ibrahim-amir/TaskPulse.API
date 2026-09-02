using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.TaskDto;

public class CreateTaskDto
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
    public DateTime? DueDate { get; set; }
    public decimal? EstimatedHours { get; set; }

    // Who the task is for. Optional — a task can be created unassigned and
    // handed out later.
    public List<int> AssignedUserIds { get; set; } = new();

    // The required-file checklist, by label. Creating these up front is what
    // gives the task a progress bar from day one.
    public List<string> SubtaskLabels { get; set; } = new();
}

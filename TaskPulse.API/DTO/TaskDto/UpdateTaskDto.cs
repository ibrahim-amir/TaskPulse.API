using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.TaskDto;

// Full edit — Manager/Admin only. A Member moving a card on the board uses
// UpdateTaskStatusDto instead, which is the only field they may change.
public class UpdateTaskDto
{
    // Filled from the route by the controller, not sent in the body.
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; }
    public PriorityLevel Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
}

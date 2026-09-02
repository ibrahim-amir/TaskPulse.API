using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.TaskDto;

// Query string for the task lists. Search matches title or description;
// paging and sorting come from PagedRequest.
public class TaskQueryDto : PagedRequest
{
    public TaskItemStatus? Status { get; set; }
    public PriorityLevel? Priority { get; set; }
    public int? AssignedUserId { get; set; }

    // Only tasks past their due date and not finished.
    public bool? OverdueOnly { get; set; }
}

using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.TaskDto;

// The Kanban drag-and-drop. Deliberately its own endpoint and its own DTO: an
// assigned Member may move their card between columns but may not touch the
// title, dates or anything else on the task.
public class UpdateTaskStatusDto
{
    // Filled from the route by the controller, not sent in the body.
    public int Id { get; set; }

    public TaskItemStatus Status { get; set; }
}

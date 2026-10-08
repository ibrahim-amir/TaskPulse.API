namespace TaskPulse.API.DTO.TaskTodoDto;

// POST /api/tasks/{taskId}/todos. TaskId comes from the route.
public class CreateTaskTodoItemDto
{
    public int TaskId { get; set; }
    public string Text { get; set; } = string.Empty;
}

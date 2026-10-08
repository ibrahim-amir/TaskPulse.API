using TaskPulse.API.Entities;

namespace TaskPulse.API.DTO.TaskTodoDto;

public class TaskTodoItemDTO
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsDone { get; set; }

    public static TaskTodoItemDTO FromEntity(TaskTodoItem item)
    {
        return new TaskTodoItemDTO
        {
            Id = item.Id,
            TaskId = item.TaskId,
            Text = item.Text,
            IsDone = item.IsDone
        };
    }
}

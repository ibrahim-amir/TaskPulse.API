namespace TaskPulse.API.DTO.TaskDto;

// Replaces the whole assignee list for a task in one call, so the frontend can
// send what the assignment panel currently shows without diffing it first.
public class AssignTaskDto
{
    // Filled from the route by the controller, not sent in the body.
    public int TaskId { get; set; }

    public List<int> UserIds { get; set; } = new();
}

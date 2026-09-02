namespace TaskPulse.API.DTO.SubtaskDto;

// Adds one more required file to an existing task.
public class CreateSubtaskDto
{
    // Filled from the route by the controller, not sent in the body.
    public int TaskId { get; set; }

    public string Label { get; set; } = string.Empty;
}

namespace TaskPulse.API.DTO.CommentDto;

public class CreateCommentDto
{
    // Filled from the route by the controller, not sent in the body.
    public int TaskId { get; set; }

    public string Body { get; set; } = string.Empty;
}

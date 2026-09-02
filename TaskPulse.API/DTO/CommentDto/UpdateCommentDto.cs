namespace TaskPulse.API.DTO.CommentDto;

public class UpdateCommentDto
{
    // Filled from the route by the controller, not sent in the body.
    public int Id { get; set; }

    public string Body { get; set; } = string.Empty;
}

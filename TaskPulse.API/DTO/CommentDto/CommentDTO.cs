using TaskPulse.API.Entities;

namespace TaskPulse.API.DTO.CommentDto;

public class CommentDTO
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Expects the User navigation to be loaded; the repository includes it.
    public static CommentDTO FromEntity(Comment comment)
    {
        return new CommentDTO
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            UserId = comment.UserId,
            UserName = comment.User?.Name ?? string.Empty,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }
}

using TaskPulse.API.Entities;

namespace TaskPulse.API.DTO.AttachmentDto;

// StoredPath is deliberately not exposed — where the file sits on the server
// is nobody's business but FileStorage's. Downloads go through the id.
public class AttachmentDTO
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int? SubtaskId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int UploadedByUserId { get; set; }
    public DateTime UploadedDate { get; set; }

    public static AttachmentDTO FromEntity(TaskAttachment attachment)
    {
        return new AttachmentDTO
        {
            Id = attachment.Id,
            TaskId = attachment.TaskId,
            SubtaskId = attachment.SubtaskId,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            FileSizeBytes = attachment.FileSizeBytes,
            UploadedByUserId = attachment.UploadedByUserId,
            UploadedDate = attachment.CreatedAt
        };
    }
}

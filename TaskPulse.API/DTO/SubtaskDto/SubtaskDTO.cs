using TaskPulse.API.Entities;

namespace TaskPulse.API.DTO.SubtaskDto;

public class SubtaskDTO
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsUploaded { get; set; }
    public int? AttachmentId { get; set; }
    public string? FileName { get; set; }
    public DateTime? UploadedDate { get; set; }

    public static SubtaskDTO FromEntity(TaskSubtask subtask)
    {
        return new SubtaskDTO
        {
            Id = subtask.Id,
            TaskId = subtask.TaskId,
            Label = subtask.Label,
            IsUploaded = subtask.IsUploaded,
            AttachmentId = subtask.AttachmentId,
            FileName = subtask.Attachment?.FileName,
            UploadedDate = subtask.UploadedDate
        };
    }
}

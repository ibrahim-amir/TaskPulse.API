using TaskPulse.API.Entities.BaseEntityClass;

namespace TaskPulse.API.Entities
{
    public class TaskAttachment : BasicEntityClass
    {
        public int TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;

        // Set when this file fills a required subtask slot;
        public int? SubtaskId { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string StoredPath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }

        public int UploadedByUserId { get; set; }
        public User UploadedByUser { get; set; } = null!;
    }
}

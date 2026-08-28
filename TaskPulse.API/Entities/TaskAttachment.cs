using TaskPulse.API.Entities.BaseEntityClass;

namespace TaskPulse.API.Entities
{
    public class TaskAttachment : BasicEntityClass
    {
        public int TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;
        public string FileName { get; set; }
        public string StoredPath { get; set; } 
        public string ContentType { get; set; } 
        public long FileSizeBytes { get; set; }
        public int UploadedByUserId { get; set; }
        public User UploadedByUser { get; set; } = null!;
    }
}

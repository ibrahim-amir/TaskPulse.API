using TaskPulse.API.Entities.BaseEntityClass;

namespace TaskPulse.API.Entities
{
    // A subtask is a required file, not a mini task: "upload the signed contract The task's progress bar is how many of these are filled.
    public class TaskSubtask : BasicEntityClass
    {
        public int TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;

        // What has to be uploaded, e.g. "Signed contract".
        public string Label { get; set; } = string.Empty;

        public bool IsUploaded { get; set; } = false;

        // The file currently filling this slot. Reuploading replaces it
        // the old attachment row is deleted, no version history.
        public int? AttachmentId { get; set; }
        public TaskAttachment? Attachment { get; set; }

        public DateTime? UploadedDate { get; set; }
    }
}

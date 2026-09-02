using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.AttachmentDto;
using TaskPulse.API.DTO.SubtaskDto;

namespace TaskPulse.API.Services.interfaces
{
    // The required-file checklist and the files that fill it. Kept apart from
    // ITaskService because this is the only part of the app that touches disk.
    public interface ITaskFileService
    {
        Task<CommonResponse<List<SubtaskDTO>>> GetSubtasksAsync(int taskId, int callerId, bool callerIsAdmin);

        Task<CommonResponse<SubtaskDTO>> AddSubtaskAsync(CreateSubtaskDto dto, int callerId, bool callerIsAdmin);

        Task<CommonResponse<string>> RemoveSubtaskAsync(int taskId, int subtaskId, int callerId, bool callerIsAdmin);

        // Fills one checklist slot. Replaces whatever was there — no history.
        // Flips the task to Review when this was the last slot outstanding.
        Task<CommonResponse<SubtaskDTO>> UploadSubtaskFileAsync(int taskId, int subtaskId, IFormFile file, int callerId, bool callerIsAdmin);

        Task<CommonResponse<List<AttachmentDTO>>> GetAttachmentsAsync(int taskId, int callerId, bool callerIsAdmin);

        // A file attached to the task generally, not tied to a checklist slot.
        Task<CommonResponse<AttachmentDTO>> UploadAttachmentAsync(int taskId, IFormFile file, int callerId, bool callerIsAdmin);

        Task<CommonResponse<string>> DeleteAttachmentAsync(int taskId, int attachmentId, int callerId, bool callerIsAdmin);

        // Returns the bytes plus the original name and content type, for the
        // controller to hand back as a file download.
        Task<CommonResponse<AttachmentDownload>> DownloadAttachmentAsync(int taskId, int attachmentId, int callerId, bool callerIsAdmin);
    }

    public class AttachmentDownload
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }
}

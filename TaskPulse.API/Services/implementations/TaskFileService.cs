using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.AttachmentDto;
using TaskPulse.API.DTO.SubtaskDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Helper.FileHelper;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Repository.TaskRepo;
using TaskPulse.API.Services.Infrastructure;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    public class TaskFileService : BaseServiceClass, ITaskFileService
    {
        private readonly TaskRepository _taskRepository;
        private readonly ProjectRepository _projectRepository;
        private readonly UploadedFileValidator _fileValidator;
        private readonly FileStorage _fileStorage;
        private readonly IActivityLogger _activityLogger;
        private readonly INotifier _notifier;

        public TaskFileService(
            CustomExceptionHandler customExceptionHandler,
            TaskRepository taskRepository,
            ProjectRepository projectRepository,
            UploadedFileValidator fileValidator,
            FileStorage fileStorage,
            IActivityLogger activityLogger,
            INotifier notifier)
            : base(customExceptionHandler)
        {
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
            _fileValidator = fileValidator;
            _fileStorage = fileStorage;
            _activityLogger = activityLogger;
            _notifier = notifier;
        }


        public async Task<CommonResponse<List<SubtaskDTO>>> GetSubtasksAsync(int taskId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<List<SubtaskDTO>>();

            try
            {
                var (_, _, error) = await LoadForReadAsync(taskId, callerId, callerIsAdmin);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<List<SubtaskDTO>>(error.Message, error.StatusCode);

                var subtasks = await _taskRepository.GetSubtasksAsync(taskId);
                return CommonResponseFactory.CreateSuccessResponse(subtasks.Select(SubtaskDTO.FromEntity).ToList(), "Checklist retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        public async Task<CommonResponse<SubtaskDTO>> AddSubtaskAsync(CreateSubtaskDto dto, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<SubtaskDTO>();

            try
            {
                if (string.IsNullOrWhiteSpace(dto.Label))
                    return CommonResponseFactory.CreateErrorResponse<SubtaskDTO>("Say what file is required.", HttpStatusCode.BadRequest);

                var (task, project, error) = await LoadForReadAsync(dto.TaskId, callerId, callerIsAdmin);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<SubtaskDTO>(error.Message, error.StatusCode);

                var access = ProjectAccess.CheckManager(project!, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<SubtaskDTO>(access, HttpStatusCode.Forbidden);

                if (task!.Status == TaskItemStatus.Done)
                    return CommonResponseFactory.CreateErrorResponse<SubtaskDTO>("This task is closed.", HttpStatusCode.BadRequest);

                var subtask = new TaskSubtask { TaskId = dto.TaskId, Label = dto.Label.Trim() };
                await _taskRepository.AddSubtaskAsync(subtask);

                await _activityLogger.LogAsync(callerId, ActivityAction.SubtaskAdded, $"Added required file '{subtask.Label}'.", task.ProjectId, task.Id);

                return CommonResponseFactory.CreateSuccessResponse(SubtaskDTO.FromEntity(subtask), CommonResponseMessage.Created.ToText());
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        public async Task<CommonResponse<string>> RemoveSubtaskAsync(int taskId, int subtaskId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<string>();

            try
            {
                var (task, project, error) = await LoadForReadAsync(taskId, callerId, callerIsAdmin);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<string>(error.Message, error.StatusCode);

                var access = ProjectAccess.CheckManager(project!, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<string>(access, HttpStatusCode.Forbidden);

                var subtask = await _taskRepository.GetSubtaskAsync(subtaskId);
                if (subtask is null || subtask.TaskId != taskId)
                    return CommonResponseFactory.CreateErrorResponse<string>("Checklist item not found on this task.", HttpStatusCode.NotFound);

                // Take the file off disk too, otherwise removing the slot
                // orphans whatever was uploaded against it.
                if (subtask.AttachmentId.HasValue)
                {
                    var attachment = await _taskRepository.GetAttachmentAsync(subtask.AttachmentId.Value);
                    if (attachment is not null)
                    {
                        subtask.AttachmentId = null;
                        await _taskRepository.UpdateSubtaskAsync(subtask);

                        _fileStorage.Delete(attachment.StoredPath);
                        await _taskRepository.RemoveAttachmentAsync(attachment);
                    }
                }

                await _taskRepository.RemoveSubtaskAsync(subtask);
                await _activityLogger.LogAsync(callerId, ActivityAction.SubtaskRemoved, $"Removed required file '{subtask.Label}'.", task!.ProjectId, task.Id);

                return CommonResponseFactory.CreateSuccessResponse("Checklist item removed.", "Checklist item removed.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Fills one checklist slot. Anyone assigned to the task can upload, as
        // can a manager of the project.
        public async Task<CommonResponse<SubtaskDTO>> UploadSubtaskFileAsync(int taskId, int subtaskId, IFormFile file, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<SubtaskDTO>();

            try
            {
                var (task, project, error) = await LoadForReadAsync(taskId, callerId, callerIsAdmin);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<SubtaskDTO>(error.Message, error.StatusCode);

                var uploadDenied = await CheckCanUploadAsync(task!, project!, callerId, callerIsAdmin);
                if (uploadDenied is not null)
                    return CommonResponseFactory.CreateErrorResponse<SubtaskDTO>(uploadDenied, HttpStatusCode.Forbidden);

                if (task!.Status == TaskItemStatus.Done)
                    return CommonResponseFactory.CreateErrorResponse<SubtaskDTO>("This task is closed — no further uploads.", HttpStatusCode.BadRequest);

                var fileErrors = _fileValidator.Validate(file);
                if (fileErrors.Count > 0)
                    return CommonResponseFactory.CreateErrorResponse<SubtaskDTO>(fileErrors, HttpStatusCode.BadRequest);

                var subtask = await _taskRepository.GetSubtaskAsync(subtaskId);
                if (subtask is null || subtask.TaskId != taskId)
                    return CommonResponseFactory.CreateErrorResponse<SubtaskDTO>("Checklist item not found on this task.", HttpStatusCode.NotFound);

                // Reupload overwrites: the previous file is detached, deleted
                // from disk, and its row removed. No version history by design.
                TaskAttachment? previous = null;
                if (subtask.AttachmentId.HasValue)
                    previous = await _taskRepository.GetAttachmentAsync(subtask.AttachmentId.Value);

                var storedPath = await _fileStorage.SaveAsync(file, taskId);

                var attachment = new TaskAttachment
                {
                    TaskId = taskId,
                    SubtaskId = subtaskId,
                    FileName = Path.GetFileName(file.FileName),
                    StoredPath = storedPath,
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Length,
                    UploadedByUserId = callerId
                };

                await _taskRepository.AddAttachmentAsync(attachment);

                subtask.AttachmentId = attachment.Id;
                subtask.IsUploaded = true;
                subtask.UploadedDate = DateTime.UtcNow;
                await _taskRepository.UpdateSubtaskAsync(subtask);

                if (previous is not null)
                {
                    _fileStorage.Delete(previous.StoredPath);
                    await _taskRepository.RemoveAttachmentAsync(previous);
                }

                await _activityLogger.LogAsync(callerId, ActivityAction.FileUploaded, $"Uploaded '{attachment.FileName}' for '{subtask.Label}'.", task.ProjectId, task.Id);

                await MoveToReviewIfChecklistCompleteAsync(task, callerId);

                var saved = await _taskRepository.GetSubtaskAsync(subtaskId);
                return CommonResponseFactory.CreateSuccessResponse(SubtaskDTO.FromEntity(saved!), "File uploaded.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        public async Task<CommonResponse<List<AttachmentDTO>>> GetAttachmentsAsync(int taskId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<List<AttachmentDTO>>();

            try
            {
                var (_, _, error) = await LoadForReadAsync(taskId, callerId, callerIsAdmin);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<List<AttachmentDTO>>(error.Message, error.StatusCode);

                var attachments = await _taskRepository.GetAttachmentsAsync(taskId);
                return CommonResponseFactory.CreateSuccessResponse(attachments.Select(AttachmentDTO.FromEntity).ToList(), "Attachments retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        public async Task<CommonResponse<AttachmentDTO>> UploadAttachmentAsync(int taskId, IFormFile file, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<AttachmentDTO>();

            try
            {
                var (task, project, error) = await LoadForReadAsync(taskId, callerId, callerIsAdmin);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<AttachmentDTO>(error.Message, error.StatusCode);

                var uploadDenied = await CheckCanUploadAsync(task!, project!, callerId, callerIsAdmin);
                if (uploadDenied is not null)
                    return CommonResponseFactory.CreateErrorResponse<AttachmentDTO>(uploadDenied, HttpStatusCode.Forbidden);

                var fileErrors = _fileValidator.Validate(file);
                if (fileErrors.Count > 0)
                    return CommonResponseFactory.CreateErrorResponse<AttachmentDTO>(fileErrors, HttpStatusCode.BadRequest);

                var storedPath = await _fileStorage.SaveAsync(file, taskId);

                var attachment = new TaskAttachment
                {
                    TaskId = taskId,
                    SubtaskId = null,
                    FileName = Path.GetFileName(file.FileName),
                    StoredPath = storedPath,
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Length,
                    UploadedByUserId = callerId
                };

                await _taskRepository.AddAttachmentAsync(attachment);
                await _activityLogger.LogAsync(callerId, ActivityAction.FileUploaded, $"Attached '{attachment.FileName}'.", task!.ProjectId, task.Id);

                return CommonResponseFactory.CreateSuccessResponse(AttachmentDTO.FromEntity(attachment), CommonResponseMessage.Created.ToText());
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        public async Task<CommonResponse<string>> DeleteAttachmentAsync(int taskId, int attachmentId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<string>();

            try
            {
                var (task, project, error) = await LoadForReadAsync(taskId, callerId, callerIsAdmin);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<string>(error.Message, error.StatusCode);

                var attachment = await _taskRepository.GetAttachmentAsync(attachmentId);
                if (attachment is null || attachment.TaskId != taskId)
                    return CommonResponseFactory.CreateErrorResponse<string>("Attachment not found on this task.", HttpStatusCode.NotFound);

                // Your own file, or a manager tidying up.
                var isManager = ProjectAccess.IsManager(project!, callerId, callerIsAdmin);
                if (!isManager && attachment.UploadedByUserId != callerId)
                    return CommonResponseFactory.CreateErrorResponse<string>("You can only remove files you uploaded.", HttpStatusCode.Forbidden);

                // Clearing a checklist slot reopens it, so the progress bar and
                // the task's status stay honest.
                if (attachment.SubtaskId.HasValue)
                {
                    var subtask = await _taskRepository.GetSubtaskAsync(attachment.SubtaskId.Value);
                    if (subtask is not null)
                    {
                        subtask.AttachmentId = null;
                        subtask.IsUploaded = false;
                        subtask.UploadedDate = null;
                        await _taskRepository.UpdateSubtaskAsync(subtask);
                    }

                    if (task!.Status == TaskItemStatus.Review)
                    {
                        task.Status = TaskItemStatus.InProgress;
                        await _taskRepository.UpdateAsync(task);
                    }
                }

                _fileStorage.Delete(attachment.StoredPath);
                await _taskRepository.RemoveAttachmentAsync(attachment);

                return CommonResponseFactory.CreateSuccessResponse("Attachment removed.", "Attachment removed.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        public async Task<CommonResponse<AttachmentDownload>> DownloadAttachmentAsync(int taskId, int attachmentId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<AttachmentDownload>();

            try
            {
                var (_, _, error) = await LoadForReadAsync(taskId, callerId, callerIsAdmin);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<AttachmentDownload>(error.Message, error.StatusCode);

                var attachment = await _taskRepository.GetAttachmentAsync(attachmentId);
                if (attachment is null || attachment.TaskId != taskId)
                    return CommonResponseFactory.CreateErrorResponse<AttachmentDownload>("Attachment not found on this task.", HttpStatusCode.NotFound);

                if (!_fileStorage.Exists(attachment.StoredPath))
                    return CommonResponseFactory.CreateErrorResponse<AttachmentDownload>("The stored file is missing.", HttpStatusCode.NotFound);

                var download = new AttachmentDownload
                {
                    Content = await File.ReadAllBytesAsync(attachment.StoredPath),
                    FileName = attachment.FileName,
                    ContentType = string.IsNullOrWhiteSpace(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType
                };

                return CommonResponseFactory.CreateSuccessResponse(download, "File ready.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Loads the task and its project, and checks the caller can see them.
        private async Task<(TaskItem? Task, Project? Project, AccessError? Error)> LoadForReadAsync(int taskId, int callerId, bool callerIsAdmin)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task is null)
                return (null, null, new AccessError("Task not found.", HttpStatusCode.NotFound));

            var project = await _projectRepository.GetByIdAsync(task.ProjectId);
            if (project is null)
                return (null, null, new AccessError("The project this task belongs to no longer exists.", HttpStatusCode.NotFound));

            var access = ProjectAccess.CheckMember(project, callerId, callerIsAdmin);
            if (access is not null)
                return (null, null, new AccessError(access, HttpStatusCode.Forbidden));

            return (task, project, null);
        }

        // Uploading is for the people doing the work plus the manager — not
        // every member of the project.
        private async Task<string?> CheckCanUploadAsync(TaskItem task, Project project, int callerId, bool callerIsAdmin)
        {
            if (ProjectAccess.IsManager(project, callerId, callerIsAdmin))
                return null;

            return await _taskRepository.IsAssignedAsync(task.Id, callerId)
                ? null
                : "Only someone assigned to this task, or a manager of the project, can upload to it.";
        }

        // The last required file landing moves the task to Review and tells the
        // project's managers there's something to check.
        private async Task MoveToReviewIfChecklistCompleteAsync(TaskItem task, int callerId)
        {
            if (task.Status == TaskItemStatus.Review || task.Status == TaskItemStatus.Done)
                return;

            if (!await _taskRepository.AllSubtasksUploadedAsync(task.Id))
                return;

            task.Status = TaskItemStatus.Review;
            await _taskRepository.UpdateAsync(task);

            var members = await _projectRepository.GetMembersAsync(task.ProjectId);
            foreach (var manager in members.Where(m => m.Role == UserRole.Manager && m.UserId != callerId))
            {
                await _notifier.NotifyAsync(manager.UserId, NotificationType.TaskDueSoon,
                    $"'{task.Title}' has all its files and is ready for review.", nameof(TaskItem), task.Id);
            }
        }

        private record AccessError(string Message, HttpStatusCode StatusCode);
    }
}

using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.CommentDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Repository.CommentRepo;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Repository.TaskRepo;
using TaskPulse.API.Services.Infrastructure;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    public class CommentService : BaseServiceClass, ICommentService
    {
        private readonly CommentRepository _commentRepository;
        private readonly TaskRepository _taskRepository;
        private readonly ProjectRepository _projectRepository;
        private readonly IActivityLogger _activityLogger;
        private readonly INotifier _notifier;

        public CommentService(
            CustomExceptionHandler customExceptionHandler,
            CommentRepository commentRepository,
            TaskRepository taskRepository,
            ProjectRepository projectRepository,
            IActivityLogger activityLogger,
            INotifier notifier)
            : base(customExceptionHandler)
        {
            _commentRepository = commentRepository;
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
            _activityLogger = activityLogger;
            _notifier = notifier;
        }


        public async Task<CommonResponse<List<CommentDTO>>> GetCommentsAsync(int taskId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<List<CommentDTO>>();

            try
            {
                var (_, _, error) = await LoadTaskForMemberAsync(taskId, callerId, callerIsAdmin);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<List<CommentDTO>>(error.Message, error.StatusCode);

                var comments = await _commentRepository.GetByTaskAsync(taskId);
                return CommonResponseFactory.CreateSuccessResponse(comments.Select(CommentDTO.FromEntity).ToList(), "Comments retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Any member of the project can comment — that's the point of comments.
        public async Task<CommonResponse<CommentDTO>> AddCommentAsync(CreateCommentDto dto, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<CommentDTO>();

            try
            {
                if (string.IsNullOrWhiteSpace(dto.Body))
                    return CommonResponseFactory.CreateErrorResponse<CommentDTO>("A comment cannot be empty.", HttpStatusCode.BadRequest);

                if (dto.Body.Length > 2000)
                    return CommonResponseFactory.CreateErrorResponse<CommentDTO>("A comment cannot exceed 2000 characters.", HttpStatusCode.BadRequest);

                var (task, _, error) = await LoadTaskForMemberAsync(dto.TaskId, callerId, callerIsAdmin);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<CommentDTO>(error.Message, error.StatusCode);

                var comment = new Comment
                {
                    TaskId = dto.TaskId,
                    UserId = callerId,
                    Body = dto.Body.Trim()
                };

                await _commentRepository.CreateAsync(comment);
                await _activityLogger.LogAsync(callerId, ActivityAction.CommentAdded, $"Commented on '{task!.Title}'.", task.ProjectId, task.Id);

                await NotifyOthersOnTaskAsync(task, callerId);

                var saved = await _commentRepository.GetByIdAsync(comment.Id);
                return CommonResponseFactory.CreateSuccessResponse(CommentDTO.FromEntity(saved!), CommonResponseMessage.Created.ToText());
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Editing is the author's alone — not even a manager rewrites someone
        // else's words.
        public async Task<CommonResponse<CommentDTO>> UpdateCommentAsync(UpdateCommentDto dto, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<CommentDTO>();

            try
            {
                if (string.IsNullOrWhiteSpace(dto.Body))
                    return CommonResponseFactory.CreateErrorResponse<CommentDTO>("A comment cannot be empty.", HttpStatusCode.BadRequest);

                var comment = await _commentRepository.GetByIdAsync(dto.Id);
                if (comment is null)
                    return CommonResponseFactory.CreateErrorResponse<CommentDTO>("Comment not found.", HttpStatusCode.NotFound);

                if (comment.UserId != callerId)
                    return CommonResponseFactory.CreateErrorResponse<CommentDTO>("You can only edit your own comments.", HttpStatusCode.Forbidden);

                comment.Body = dto.Body.Trim();
                await _commentRepository.UpdateAsync(comment);
                await _activityLogger.LogAsync(callerId, ActivityAction.CommentUpdated, "Edited a comment.", comment.Task?.ProjectId, comment.TaskId);

                return CommonResponseFactory.CreateSuccessResponse(CommentDTO.FromEntity(comment), "Comment updated.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Deleting is the author's, or a manager of the project moderating.
        public async Task<CommonResponse<string>> DeleteCommentAsync(int id, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<string>();

            try
            {
                var comment = await _commentRepository.GetByIdAsync(id);
                if (comment is null)
                    return CommonResponseFactory.CreateErrorResponse<string>("Comment not found.", HttpStatusCode.NotFound);

                var isAuthor = comment.UserId == callerId;
                var isManager = false;

                if (comment.Task is not null)
                {
                    var project = await _projectRepository.GetByIdAsync(comment.Task.ProjectId);
                    isManager = project is not null && ProjectAccess.IsManager(project, callerId, callerIsAdmin);
                }

                if (!isAuthor && !isManager)
                    return CommonResponseFactory.CreateErrorResponse<string>("You can only delete your own comments.", HttpStatusCode.Forbidden);

                await _commentRepository.DeleteAsync(comment);
                await _activityLogger.LogAsync(callerId, ActivityAction.CommentDeleted, "Deleted a comment.", comment.Task?.ProjectId, comment.TaskId);

                return CommonResponseFactory.CreateSuccessResponse("Comment deleted.", "Comment deleted.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        private async Task<(TaskItem? Task, Project? Project, AccessError? Error)> LoadTaskForMemberAsync(int taskId, int callerId, bool callerIsAdmin)
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

        // Everyone assigned to the task hears about a new comment, except
        // whoever wrote it.
        private async Task NotifyOthersOnTaskAsync(TaskItem task, int callerId)
        {
            var assigneeIds = await _taskRepository.GetAssigneeIdsAsync(task.Id);

            foreach (var userId in assigneeIds.Where(userId => userId != callerId))
            {
                await _notifier.NotifyAsync(userId, NotificationType.CommentAdded,
                    $"New comment on '{task.Title}'.", nameof(TaskItem), task.Id);
            }
        }

        private record AccessError(string Message, HttpStatusCode StatusCode);
    }
}

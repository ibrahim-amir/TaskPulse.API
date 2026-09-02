using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Repository.TaskRepo;
using TaskPulse.API.Services.Infrastructure;
using TaskPulse.API.Services.interfaces;
using TaskPulse.API.Validation;

namespace TaskPulse.API.Services.implementations
{
    public class TaskService : BaseServiceClass, ITaskService
    {
        private readonly CheckValidationRun _validationRunner;
        private readonly TaskRepository _taskRepository;
        private readonly ProjectRepository _projectRepository;
        private readonly IActivityLogger _activityLogger;
        private readonly INotifier _notifier;

        public TaskService(
            CustomExceptionHandler customExceptionHandler,
            CheckValidationRun validationRunner,
            TaskRepository taskRepository,
            ProjectRepository projectRepository,
            IActivityLogger activityLogger,
            INotifier notifier)
            : base(customExceptionHandler)
        {
            _validationRunner = validationRunner;
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
            _activityLogger = activityLogger;
            _notifier = notifier;
        }


        // Get Tasks Service Method — projectId null lists across every project
        // the caller can see, which is what "my tasks" and the dashboard use.
        public async Task<CommonResponse<PagedResult<TaskDTO>>> GetTasksAsync(TaskQueryDto query, int? projectId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<PagedResult<TaskDTO>>();

            try
            {
                if (projectId.HasValue)
                {
                    var project = await _projectRepository.GetByIdAsync(projectId.Value);
                    if (project is null)
                        return CommonResponseFactory.CreateErrorResponse<PagedResult<TaskDTO>>("Project not found.", HttpStatusCode.NotFound);

                    var access = ProjectAccess.CheckMember(project, callerId, callerIsAdmin);
                    if (access is not null)
                        return CommonResponseFactory.CreateErrorResponse<PagedResult<TaskDTO>>(access, HttpStatusCode.Forbidden);
                }

                var page = await _taskRepository.GetPagedAsync(query, projectId, callerIsAdmin ? null : callerId);
                return CommonResponseFactory.CreateSuccessResponse(page.MapItems(TaskDTO.FromEntity), "Tasks retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Get Task By Id Service Method
        public async Task<CommonResponse<TaskDTO>> GetTaskByIdAsync(int id, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<TaskDTO>();

            try
            {
                var (task, project, error) = await LoadTaskAndProjectAsync(id);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(error, HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckMember(project!, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(access, HttpStatusCode.Forbidden);

                return CommonResponseFactory.CreateSuccessResponse(TaskDTO.FromEntity(task!), "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Create Task Service Method — the required-file checklist and the
        // assignees can both be set up in the same call.
        public async Task<CommonResponse<TaskDTO>> CreateTaskAsync(CreateTaskDto dto, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<TaskDTO>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(validation.Errors, validation.StatusCode);

                var project = await _projectRepository.GetByIdAsync(dto.ProjectId);
                if (project is null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>("Project not found.", HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckManager(project, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(access, HttpStatusCode.Forbidden);

                var assigneeError = EnsureAssigneesAreMembers(project, dto.AssignedUserIds);
                if (assigneeError is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(assigneeError, HttpStatusCode.BadRequest);

                var task = new TaskItem
                {
                    ProjectId = dto.ProjectId,
                    Title = dto.Title,
                    Description = dto.Description,
                    Priority = dto.Priority,
                    DueDate = dto.DueDate,
                    EstimatedHours = dto.EstimatedHours,
                    CreatedByUserId = callerId,
                    Status = TaskItemStatus.ToDo
                };

                await _taskRepository.CreateAsync(task);

                if (dto.SubtaskLabels.Count > 0)
                {
                    var subtasks = dto.SubtaskLabels
                        .Where(label => !string.IsNullOrWhiteSpace(label))
                        .Select(label => new TaskSubtask { TaskId = task.Id, Label = label.Trim() });

                    await _taskRepository.AddSubtasksAsync(subtasks);
                }

                if (dto.AssignedUserIds.Count > 0)
                {
                    await _taskRepository.ReplaceAssignmentsAsync(task, dto.AssignedUserIds, CallerRole(callerIsAdmin));
                    await NotifyAssigneesAsync(dto.AssignedUserIds, task, callerId);
                }

                await _activityLogger.LogAsync(callerId, ActivityAction.TaskCreated, $"Created task '{task.Title}'.", task.ProjectId, task.Id);

                var saved = await _taskRepository.GetByIdAsync(task.Id);
                return CommonResponseFactory.CreateSuccessResponse(TaskDTO.FromEntity(saved!), CommonResponseMessage.Created.ToText());
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Update Task Service Method — full edit, Manager/Admin only.
        public async Task<CommonResponse<TaskDTO>> UpdateTaskAsync(UpdateTaskDto dto, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<TaskDTO>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(validation.Errors, validation.StatusCode);

                var (task, project, error) = await LoadTaskAndProjectAsync(dto.Id);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(error, HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckManager(project!, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(access, HttpStatusCode.Forbidden);

                task!.Title = dto.Title;
                task.Description = dto.Description;
                task.Status = dto.Status;
                task.Priority = dto.Priority;
                task.EstimatedHours = dto.EstimatedHours;
                task.ActualHours = dto.ActualHours;

                // Moving the deadline gives the monitor a fresh start, otherwise
                // an already-warned task stays silent on its new date.
                if (task.DueDate != dto.DueDate)
                {
                    task.DueDate = dto.DueDate;
                    task.DueSoonNotified = false;
                    task.OverdueNotified = false;
                }

                await _taskRepository.UpdateAsync(task);
                await _activityLogger.LogAsync(callerId, ActivityAction.TaskUpdated, $"Updated task '{task.Title}'.", task.ProjectId, task.Id);

                return CommonResponseFactory.CreateSuccessResponse(TaskDTO.FromEntity(task), "Task updated.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Update Task Status Service Method — the board drag. An assigned
        // Member may do this; changing anything else still needs a Manager.
        public async Task<CommonResponse<TaskDTO>> UpdateTaskStatusAsync(UpdateTaskStatusDto dto, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<TaskDTO>();

            try
            {
                var (task, project, error) = await LoadTaskAndProjectAsync(dto.Id);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(error, HttpStatusCode.NotFound);

                var isManager = ProjectAccess.IsManager(project!, callerId, callerIsAdmin);
                var isAssignee = await _taskRepository.IsAssignedAsync(task!.Id, callerId);

                if (!isManager && !isAssignee)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(
                        "Only a manager of this project or someone assigned to the task can move it.", HttpStatusCode.Forbidden);

                // Done is reached by closing the task after review, not by
                // dragging a card into the column.
                if (dto.Status == TaskItemStatus.Done && !isManager)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(
                        "A manager closes the task once the uploads have been reviewed.", HttpStatusCode.Forbidden);

                task.Status = dto.Status;
                await _taskRepository.UpdateAsync(task);
                await _activityLogger.LogAsync(callerId, ActivityAction.TaskStatusChanged, $"Moved '{task.Title}' to {dto.Status}.", task.ProjectId, task.Id);

                return CommonResponseFactory.CreateSuccessResponse(TaskDTO.FromEntity(task), $"Task moved to {dto.Status}.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Assign Task Service Method — replaces the assignee list wholesale.
        public async Task<CommonResponse<TaskDTO>> AssignTaskAsync(AssignTaskDto dto, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<TaskDTO>();

            try
            {
                var (task, project, error) = await LoadTaskAndProjectAsync(dto.TaskId);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(error, HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckManager(project!, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(access, HttpStatusCode.Forbidden);

                var assigneeError = EnsureAssigneesAreMembers(project!, dto.UserIds);
                if (assigneeError is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(assigneeError, HttpStatusCode.BadRequest);

                var before = await _taskRepository.GetAssigneeIdsAsync(task!.Id);
                await _taskRepository.ReplaceAssignmentsAsync(task, dto.UserIds, CallerRole(callerIsAdmin));

                //Only tell people who weren't already on it.
                await NotifyAssigneesAsync(dto.UserIds.Except(before).ToList(), task, callerId);

                await _activityLogger.LogAsync(callerId, ActivityAction.TaskAssigned, $"Set assignees on '{task.Title}'.", task.ProjectId, task.Id);

                var saved = await _taskRepository.GetByIdAsync(task.Id);
                return CommonResponseFactory.CreateSuccessResponse(TaskDTO.FromEntity(saved!), "Assignees updated.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Close Task Service Method — the review step. Everything required has
        // to be uploaded first, and after this no more reuploads are accepted.
        public async Task<CommonResponse<TaskDTO>> CloseTaskAsync(int id, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<TaskDTO>();

            try
            {
                var (task, project, error) = await LoadTaskAndProjectAsync(id);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(error, HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckManager(project!, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(access, HttpStatusCode.Forbidden);

                if (task!.Status == TaskItemStatus.Done)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>("This task is already closed.", HttpStatusCode.BadRequest);

                var required = task.Subtasks.Count;
                var uploaded = task.Subtasks.Count(s => s.IsUploaded);
                if (required > 0 && uploaded < required)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(
                        $"{uploaded} of {required} required files have been uploaded.", HttpStatusCode.BadRequest);

                task.Status = TaskItemStatus.Done;
                task.CompletedDate = DateTime.UtcNow;
                await _taskRepository.UpdateAsync(task);

                await _activityLogger.LogAsync(callerId, ActivityAction.TaskClosed, $"Closed task '{task.Title}'.", task.ProjectId, task.Id);

                return CommonResponseFactory.CreateSuccessResponse(TaskDTO.FromEntity(task), "Task closed.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Delete Task Service Method
        public async Task<CommonResponse<string>> DeleteTaskAsync(int id, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<string>();

            try
            {
                var (task, project, error) = await LoadTaskAndProjectAsync(id);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<string>(error, HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckManager(project!, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<string>(access, HttpStatusCode.Forbidden);

                var title = task!.Title;
                var projectId = task.ProjectId;

                await _taskRepository.DeleteAsync(task);
                await _activityLogger.LogAsync(callerId, ActivityAction.TaskDeleted, $"Deleted task '{title}'.", projectId);

                return CommonResponseFactory.CreateSuccessResponse("Task deleted.", "Task deleted.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Every method starts by loading the task and its project together —
        // the project is what the permission rules are written against.
        private async Task<(TaskItem? Task, Project? Project, string? Error)> LoadTaskAndProjectAsync(int taskId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task is null)
                return (null, null, "Task not found.");

            var project = await _projectRepository.GetByIdAsync(task.ProjectId);
            if (project is null)
                return (null, null, "The project this task belongs to no longer exists.");

            return (task, project, null);
        }

        // You can only be given work on a project you're actually on.
        private static string? EnsureAssigneesAreMembers(Project project, List<int> userIds)
        {
            var outsiders = userIds
                .Distinct()
                .Where(userId => !project.Members.Any(m => m.UserId == userId))
                .ToList();

            return outsiders.Count == 0
                ? null
                : $"These users are not members of this project: {string.Join(", ", outsiders)}.";
        }

        private static UserRole CallerRole(bool callerIsAdmin)
        {
            return callerIsAdmin ? UserRole.Admin : UserRole.Manager;
        }

        private async Task NotifyAssigneesAsync(List<int> userIds, TaskItem task, int callerId)
        {
            foreach (var userId in userIds.Distinct().Where(userId => userId != callerId))
            {
                await _notifier.NotifyAsync(userId, NotificationType.TaskAssigned,
                    $"You were assigned '{task.Title}'.", nameof(TaskItem), task.Id);
            }
        }
    }
}

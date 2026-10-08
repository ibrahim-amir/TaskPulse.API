using System.Net;
using System.Text.Json;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.ApprovalDto;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.TaskApprovalDto;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.DTO.TaskTodoDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Repository.ApprovalRepo;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Repository.TaskApprovalRepo;
using TaskPulse.API.Repository.TaskRepo;
using TaskPulse.API.Repository.UserRepo;
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
        private readonly TaskApprovalRequestRepository _taskApprovalRepository;
        private readonly ApprovalRequestRepository _approvalRepository;
        private readonly UserRepository _userRepository;
        private readonly IActivityLogger _activityLogger;
        private readonly INotifier _notifier;
        private readonly IApprovalGate _approvalGate;
        private readonly IApprovalService _approvalService;

        public TaskService(
            CustomExceptionHandler customExceptionHandler,
            CheckValidationRun validationRunner,
            TaskRepository taskRepository,
            ProjectRepository projectRepository,
            TaskApprovalRequestRepository taskApprovalRepository,
            ApprovalRequestRepository approvalRepository,
            UserRepository userRepository,
            IActivityLogger activityLogger,
            INotifier notifier,
            IApprovalGate approvalGate,
            IApprovalService approvalService)
            : base(customExceptionHandler)
        {
            _validationRunner = validationRunner;
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
            _taskApprovalRepository = taskApprovalRepository;
            _approvalRepository = approvalRepository;
            _userRepository = userRepository;
            _activityLogger = activityLogger;
            _notifier = notifier;
            _approvalGate = approvalGate;
            _approvalService = approvalService;
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
                    var (immediate, busy) = await SplitByAvailabilityAsync(dto.AssignedUserIds, callerIsAdmin);
                    await _taskRepository.ReplaceAssignmentsAsync(task, immediate, CallerRole(callerIsAdmin));
                    await NotifyAssigneesAsync(immediate, task, callerId);
                    await RequestBusyAssignmentsAsync(task, busy, callerId);
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


        // Update Task Service Method — any project member can propose an edit;
        // it applies immediately for the task's creator (or an Admin), and goes
        // through the creator-resolved edit-request gate for everyone else.
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

                var access = ProjectAccess.CheckMember(project!, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(access, HttpStatusCode.Forbidden);

                var isCreator = task!.CreatedByUserId == callerId;
                var needsApproval = !callerIsAdmin && !isCreator;

                var gate = await _approvalGate.ExecuteOrRequestApprovalAsync(
                    ApprovalActionType.UpdateTask,
                    nameof(TaskItem),
                    task.Id,
                    needsApproval,
                    callerId,
                    execute: () => ApplyTaskEditsAsync(task, dto, callerId),
                    payload: dto);

                if (!gate.Executed)
                {
                    if (!gate.AlreadyPending)
                    {
                        // The generic gate only pages Admins — the actual
                        // approver here is the task's creator.
                        await _notifier.NotifyAsync(task.CreatedByUserId, NotificationType.ApprovalRequired,
                            $"A change to '{task.Title}' is waiting for your approval.", nameof(TaskItem), task.Id);
                    }

                    var message = gate.AlreadyPending
                        ? "An edit request for this task is already pending."
                        : "Requested — the task's creator will need to approve your changes.";
                    return CommonResponseFactory.CreateSuccessResponse(TaskDTO.FromEntity(task), message);
                }

                return CommonResponseFactory.CreateSuccessResponse(TaskDTO.FromEntity(task), "Task updated.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        private async Task ApplyTaskEditsAsync(TaskItem task, UpdateTaskDto dto, int callerId)
        {
            task.Title = dto.Title;
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

                // Someone already on the task stays on it regardless of their
                // current busy flag — only a NEW addition is gated.
                var staying = dto.UserIds.Intersect(before).ToList();
                var newlyAdded = dto.UserIds.Except(before).ToList();
                var (newlyImmediate, busy) = await SplitByAvailabilityAsync(newlyAdded, callerIsAdmin);

                await _taskRepository.ReplaceAssignmentsAsync(task, staying.Concat(newlyImmediate).ToList(), CallerRole(callerIsAdmin));

                //Only tell people who weren't already on it.
                await NotifyAssigneesAsync(newlyImmediate, task, callerId);
                await RequestBusyAssignmentsAsync(task, busy, callerId);

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


        // Submit For Review Service Method — the Member-facing version of
        // CloseTaskAsync. Same "everything required is uploaded" precondition,
        // but files an ApprovalRequest for the Admin instead of closing the
        // task directly. CompleteTaskApprovalHandler does the actual closing.
        public async Task<CommonResponse<string>> SubmitForReviewAsync(int id, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<string>();

            try
            {
                var (task, project, error) = await LoadTaskAndProjectAsync(id);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<string>(error, HttpStatusCode.NotFound);

                var isManager = ProjectAccess.IsManager(project!, callerId, callerIsAdmin);
                var isAssignee = await _taskRepository.IsAssignedAsync(task!.Id, callerId);
                if (!isManager && !isAssignee)
                    return CommonResponseFactory.CreateErrorResponse<string>(
                        "Only someone assigned to this task (or its manager) can submit it for review.", HttpStatusCode.Forbidden);

                if (task.Status == TaskItemStatus.Done)
                    return CommonResponseFactory.CreateErrorResponse<string>("This task is already closed.", HttpStatusCode.BadRequest);

                var required = task.Subtasks.Count;
                var uploaded = task.Subtasks.Count(s => s.IsUploaded);
                if (required > 0 && uploaded < required)
                    return CommonResponseFactory.CreateErrorResponse<string>(
                        $"{uploaded} of {required} required files have been uploaded.", HttpStatusCode.BadRequest);

                var gate = await _approvalGate.ExecuteOrRequestApprovalAsync(
                    ApprovalActionType.CompleteTask,
                    nameof(TaskItem),
                    task.Id,
                    needsApproval: true,
                    requestedByUserId: callerId,
                    execute: () => Task.CompletedTask);

                if (gate.AlreadyPending)
                    return CommonResponseFactory.CreateSuccessResponse("A review request for this task is already pending.", "Already pending.");

                task.Status = TaskItemStatus.Review;
                await _taskRepository.UpdateAsync(task);

                return CommonResponseFactory.CreateSuccessResponse("Submitted for review.", "Submitted for review.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Delete Task Service Method
        // Open to any project member, same as UpdateTaskAsync — applies
        // immediately for the task's creator (or an Admin); for anyone else
        // it's queued as a creator-resolved deletion request instead.
        public async Task<CommonResponse<string>> DeleteTaskAsync(int id, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<string>();

            try
            {
                var (task, project, error) = await LoadTaskAndProjectAsync(id);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<string>(error, HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckMember(project!, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<string>(access, HttpStatusCode.Forbidden);

                var title = task!.Title;
                var projectId = task.ProjectId;
                var isCreator = task.CreatedByUserId == callerId;
                var needsApproval = !callerIsAdmin && !isCreator;

                var gate = await _approvalGate.ExecuteOrRequestApprovalAsync(
                    ApprovalActionType.DeleteTask,
                    nameof(TaskItem),
                    task.Id,
                    needsApproval,
                    callerId,
                    execute: () => _taskRepository.DeleteAsync(task));

                if (!gate.Executed)
                {
                    if (!gate.AlreadyPending)
                    {
                        // The generic gate only pages Admins — the actual
                        // approver here is the task's creator.
                        await _notifier.NotifyAsync(task.CreatedByUserId, NotificationType.ApprovalRequired,
                            $"A request to delete '{title}' is waiting for your approval.", nameof(TaskItem), task.Id);
                    }

                    var pendingMessage = gate.AlreadyPending
                        ? "A deletion request for this task is already pending."
                        : "Requested — the task's creator will need to approve deleting this task.";
                    return CommonResponseFactory.CreateSuccessResponse(pendingMessage, pendingMessage);
                }

                await _activityLogger.LogAsync(callerId, ActivityAction.TaskDeleted, $"Deleted task '{title}'.", projectId);

                return CommonResponseFactory.CreateSuccessResponse("Task deleted.", "Task deleted.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Duplicates an existing task with the given (editable) field values.
        // Unlike a blank CreateTaskAsync, this can only ever reproduce a task
        // that's already visible to the caller, so it's open to any project
        // member rather than Manager/Admin only. A Manager/Admin's own
        // duplicate is added right away; anyone else's needs that project's
        // Manager (or an Admin) to approve it first — see the
        // duplicate-requests endpoints and DuplicateTaskApprovalHandler,
        // which does the actual creation once approved.
        public async Task<CommonResponse<TaskDTO>> DuplicateTaskAsync(int sourceTaskId, CreateTaskDto dto, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<TaskDTO>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(validation.Errors, validation.StatusCode);

                var source = await _taskRepository.GetByIdAsync(sourceTaskId);
                if (source is null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>("Source task not found.", HttpStatusCode.NotFound);

                var project = await _projectRepository.GetByIdAsync(dto.ProjectId);
                if (project is null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>("Project not found.", HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckMember(project, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(access, HttpStatusCode.Forbidden);

                var assigneeError = EnsureAssigneesAreMembers(project, dto.AssignedUserIds);
                if (assigneeError is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(assigneeError, HttpStatusCode.BadRequest);

                var isManager = ProjectAccess.IsManager(project, callerId, callerIsAdmin);
                TaskItem? created = null;

                var gate = await _approvalGate.ExecuteOrRequestApprovalAsync(
                    ApprovalActionType.DuplicateTask,
                    nameof(TaskItem),
                    sourceTaskId,
                    needsApproval: !isManager,
                    requestedByUserId: callerId,
                    payload: dto,
                    execute: async () =>
                    {
                        created = new TaskItem
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

                        await _taskRepository.CreateAsync(created);

                        if (dto.SubtaskLabels.Count > 0)
                        {
                            var subtasks = dto.SubtaskLabels
                                .Where(label => !string.IsNullOrWhiteSpace(label))
                                .Select(label => new TaskSubtask { TaskId = created.Id, Label = label.Trim() });

                            await _taskRepository.AddSubtasksAsync(subtasks);
                        }

                        if (dto.AssignedUserIds.Count > 0)
                        {
                            var (immediate, busy) = await SplitByAvailabilityAsync(dto.AssignedUserIds, callerIsAdmin);
                            await _taskRepository.ReplaceAssignmentsAsync(created, immediate, CallerRole(callerIsAdmin));
                            await NotifyAssigneesAsync(immediate, created, callerId);
                            await RequestBusyAssignmentsAsync(created, busy, callerId);
                        }

                        await _activityLogger.LogAsync(callerId, ActivityAction.TaskCreated, $"Duplicated task '{source.Title}' as '{created.Title}'.", created.ProjectId, created.Id);
                    });

                if (!gate.Executed)
                {
                    if (!gate.AlreadyPending)
                    {
                        // The generic gate only pages Admins — the actual
                        // approvers here are this project's Managers too.
                        foreach (var managerId in project.Members.Where(m => m.Role == UserRole.Manager).Select(m => m.UserId).Distinct())
                            await _notifier.NotifyAsync(managerId, NotificationType.ApprovalRequired,
                                $"A request to duplicate '{source.Title}' is waiting for your approval.", nameof(TaskItem), sourceTaskId);
                    }

                    var pendingMessage = gate.AlreadyPending
                        ? "A duplicate request for this task is already pending."
                        : "Requested — a manager or admin will need to approve adding this copy.";
                    return CommonResponseFactory.CreateSuccessResponse<TaskDTO>(null!, pendingMessage);
                }

                var saved = await _taskRepository.GetByIdAsync(created!.Id);
                return CommonResponseFactory.CreateSuccessResponse(TaskDTO.FromEntity(saved!), "Task duplicated.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Files a submission that needs sign-off from every one of the tagged
        // approvers — not just any Admin. Only one request can be in flight
        // (Pending, or Approved-but-not-yet-submitted) per task at a time.
        public async Task<CommonResponse<TaskApprovalRequestDTO>> RequestTaskApprovalAsync(int taskId, int callerId, bool callerIsAdmin, CreateTaskApprovalRequestDto dto)
        {
            var response = new CommonResponse<TaskApprovalRequestDTO>();

            try
            {
                var (task, project, error) = await LoadTaskAndProjectAsync(taskId);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskApprovalRequestDTO>(error, HttpStatusCode.NotFound);

                var isManager = ProjectAccess.IsManager(project!, callerId, callerIsAdmin);
                var isAssignee = await _taskRepository.IsAssignedAsync(task!.Id, callerId);
                if (!isManager && !isAssignee)
                    return CommonResponseFactory.CreateErrorResponse<TaskApprovalRequestDTO>(
                        "Only someone assigned to this task (or its manager) can request approval.", HttpStatusCode.Forbidden);

                if (task.Status == TaskItemStatus.Done)
                    return CommonResponseFactory.CreateErrorResponse<TaskApprovalRequestDTO>("This task is already closed.", HttpStatusCode.BadRequest);

                var required = task.Subtasks.Count;
                var uploaded = task.Subtasks.Count(s => s.IsUploaded);
                if (required > 0 && uploaded < required)
                    return CommonResponseFactory.CreateErrorResponse<TaskApprovalRequestDTO>(
                        $"{uploaded} of {required} required files have been uploaded.", HttpStatusCode.BadRequest);

                var approverIds = dto.ApproverUserIds.Distinct().ToList();
                if (approverIds.Count == 0)
                    return CommonResponseFactory.CreateErrorResponse<TaskApprovalRequestDTO>(
                        "Tag at least one person to approve this submission.", HttpStatusCode.BadRequest);

                var existing = await _taskApprovalRepository.GetLatestForTaskAsync(taskId);
                if (existing is not null && existing.Status != ApprovalStatus.Rejected)
                    return CommonResponseFactory.CreateErrorResponse<TaskApprovalRequestDTO>(
                        existing.Status == ApprovalStatus.Approved
                            ? "Every approver already signed off — click Submit instead of requesting again."
                            : "An approval request for this task is already pending.",
                        HttpStatusCode.BadRequest);

                var request = new TaskApprovalRequest
                {
                    TaskId = taskId,
                    RequestedByUserId = callerId,
                    Description = dto.Description,
                    Status = ApprovalStatus.Pending,
                    Responses = approverIds
                        .Select(userId => new TaskApprovalResponse { ApproverUserId = userId })
                        .ToList()
                };

                await _taskApprovalRepository.CreateAsync(request);

                task.Status = TaskItemStatus.Review;
                await _taskRepository.UpdateAsync(task);

                var note = string.IsNullOrWhiteSpace(dto.Description) ? "" : $" \"{dto.Description}\"";
                foreach (var approverId in approverIds)
                {
                    await _notifier.NotifyAsync(
                        approverId,
                        NotificationType.ApprovalRequired,
                        $"Your approval is requested to submit '{task.Title}' as complete.{note}",
                        nameof(TaskItem),
                        task.Id);
                }

                await _activityLogger.LogAsync(callerId, ActivityAction.ApprovalRequested, $"Requested approval to submit '{task.Title}'.", task.ProjectId, task.Id);

                var saved = await _taskApprovalRepository.GetByIdAsync(request.Id);
                return CommonResponseFactory.CreateSuccessResponse(TaskApprovalRequestDTO.FromEntity(saved!), "Approval requested.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        // Null data (not an error) when the task has never had one — that's
        // the normal state before the first request is filed.
        public async Task<CommonResponse<TaskApprovalRequestDTO?>> GetTaskApprovalStatusAsync(int taskId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<TaskApprovalRequestDTO?>();

            try
            {
                var (task, project, error) = await LoadTaskAndProjectAsync(taskId);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskApprovalRequestDTO?>(error, HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckMember(project!, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskApprovalRequestDTO?>(access, HttpStatusCode.Forbidden);

                var latest = await _taskApprovalRepository.GetLatestForTaskAsync(taskId);
                var data = latest is null ? null : TaskApprovalRequestDTO.FromEntity(latest);
                return CommonResponseFactory.CreateSuccessResponse(data, latest is null ? "No approval request yet." : "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        // Each tagged approver answers for themselves — found by matching the
        // caller's id against the response rows, not by an id in the route.
        public async Task<CommonResponse<TaskApprovalRequestDTO>> RespondToTaskApprovalAsync(int requestId, int callerId, RespondToTaskApprovalDto dto)
        {
            var response = new CommonResponse<TaskApprovalRequestDTO>();

            try
            {
                var request = await _taskApprovalRepository.GetByIdAsync(requestId);
                if (request is null)
                    return CommonResponseFactory.CreateErrorResponse<TaskApprovalRequestDTO>("Approval request not found.", HttpStatusCode.NotFound);

                if (request.Status != ApprovalStatus.Pending)
                    return CommonResponseFactory.CreateErrorResponse<TaskApprovalRequestDTO>(
                        $"This request was already {request.Status.ToString().ToLower()}.", HttpStatusCode.BadRequest);

                var myResponse = request.Responses.FirstOrDefault(r => r.ApproverUserId == callerId);
                if (myResponse is null)
                    return CommonResponseFactory.CreateErrorResponse<TaskApprovalRequestDTO>(
                        "You were not asked to approve this submission.", HttpStatusCode.Forbidden);

                if (myResponse.Status != ApprovalStatus.Pending)
                    return CommonResponseFactory.CreateErrorResponse<TaskApprovalRequestDTO>(
                        "You already responded to this request.", HttpStatusCode.BadRequest);

                myResponse.Status = dto.Approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
                myResponse.RespondedDate = DateTime.UtcNow;
                myResponse.Note = dto.Note;

                if (request.Responses.Any(r => r.Status == ApprovalStatus.Rejected))
                {
                    request.Status = ApprovalStatus.Rejected;
                    request.ResolvedDate = DateTime.UtcNow;
                    await _notifier.NotifyAsync(
                        request.RequestedByUserId,
                        NotificationType.ApprovalResolved,
                        $"{myResponse.ApproverUser?.Name ?? "An approver"} declined the submission of '{request.Task.Title}'.",
                        nameof(TaskItem),
                        request.TaskId);
                }
                else if (request.Responses.All(r => r.Status == ApprovalStatus.Approved))
                {
                    request.Status = ApprovalStatus.Approved;
                    request.ResolvedDate = DateTime.UtcNow;
                    await _notifier.NotifyAsync(
                        request.RequestedByUserId,
                        NotificationType.ApprovalResolved,
                        $"Everyone approved '{request.Task.Title}' — you can now submit it.",
                        nameof(TaskItem),
                        request.TaskId);
                }

                await _taskApprovalRepository.UpdateAsync(request);

                var action = dto.Approved ? ActivityAction.ApprovalApproved : ActivityAction.ApprovalRejected;
                await _activityLogger.LogAsync(callerId, action, $"{(dto.Approved ? "Approved" : "Rejected")} submission of '{request.Task.Title}'.", request.Task.ProjectId, request.TaskId);

                return CommonResponseFactory.CreateSuccessResponse(TaskApprovalRequestDTO.FromEntity(request), "Response recorded.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        // The final, explicit step — only unlocked once every tagged approver
        // has said yes. Mirrors CloseTaskAsync's completion logic.
        public async Task<CommonResponse<string>> SubmitApprovedTaskAsync(int taskId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<string>();

            try
            {
                var (task, project, error) = await LoadTaskAndProjectAsync(taskId);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<string>(error, HttpStatusCode.NotFound);

                var isManager = ProjectAccess.IsManager(project!, callerId, callerIsAdmin);
                var isAssignee = await _taskRepository.IsAssignedAsync(task!.Id, callerId);
                if (!isManager && !isAssignee)
                    return CommonResponseFactory.CreateErrorResponse<string>(
                        "Only someone assigned to this task (or its manager) can submit it.", HttpStatusCode.Forbidden);

                if (task.Status == TaskItemStatus.Done)
                    return CommonResponseFactory.CreateErrorResponse<string>("This task is already closed.", HttpStatusCode.BadRequest);

                var latest = await _taskApprovalRepository.GetLatestForTaskAsync(taskId);
                if (latest is null || latest.Status != ApprovalStatus.Approved)
                    return CommonResponseFactory.CreateErrorResponse<string>(
                        "This task's submission hasn't been approved by everyone yet.", HttpStatusCode.BadRequest);

                task.Status = TaskItemStatus.Done;
                task.CompletedDate = DateTime.UtcNow;
                await _taskRepository.UpdateAsync(task);

                var otherAssigneeIds = task.Assignments.Select(a => a.UserId).Distinct().Where(userId => userId != callerId);
                foreach (var userId in otherAssigneeIds)
                {
                    await _notifier.NotifyAsync(
                        userId,
                        NotificationType.ApprovalResolved,
                        $"Task '{task.Title}' was approved and marked complete.",
                        nameof(TaskItem),
                        task.Id);
                }

                await _activityLogger.LogAsync(callerId, ActivityAction.TaskClosed, $"Submitted '{task.Title}' as complete after approval.", task.ProjectId, task.Id);

                return CommonResponseFactory.CreateSuccessResponse("Task submitted and marked complete.", "Submitted.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<TaskDTO>> ReopenTaskAsync(int taskId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<TaskDTO>();

            try
            {
                var (task, project, error) = await LoadTaskAndProjectAsync(taskId);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(error, HttpStatusCode.NotFound);

                var isManager = ProjectAccess.IsManager(project!, callerId, callerIsAdmin);
                var isAssignee = await _taskRepository.IsAssignedAsync(task!.Id, callerId);
                if (!isManager && !isAssignee)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>(
                        "Only someone assigned to this task (or its manager) can reopen it.", HttpStatusCode.Forbidden);

                if (task.Status != TaskItemStatus.Done)
                    return CommonResponseFactory.CreateErrorResponse<TaskDTO>("Only a completed task can be reopened.", HttpStatusCode.BadRequest);

                task.Status = TaskItemStatus.InProgress;
                task.CompletedDate = null;
                await _taskRepository.UpdateAsync(task);

                // An already-Approved request would otherwise let Submit fire
                // immediately with no fresh review — retire it so reopening
                // really does mean starting the approval cycle over.
                var latestApproval = await _taskApprovalRepository.GetLatestForTaskAsync(taskId);
                if (latestApproval is not null && latestApproval.Status == ApprovalStatus.Approved)
                {
                    latestApproval.Status = ApprovalStatus.Rejected;
                    latestApproval.ResolvedDate = DateTime.UtcNow;
                    await _taskApprovalRepository.UpdateAsync(latestApproval);
                }

                await _activityLogger.LogAsync(callerId, ActivityAction.TaskStatusChanged, $"Reopened '{task.Title}'.", task.ProjectId, task.Id);

                var saved = await _taskRepository.GetByIdAsync(task.Id);
                return CommonResponseFactory.CreateSuccessResponse(TaskDTO.FromEntity(saved!), "Task reopened.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<string>> RequestAssignmentAsync(int taskId, int callerId)
        {
            var response = new CommonResponse<string>();

            try
            {
                var (task, _, error) = await LoadTaskAndProjectAsync(taskId);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<string>(error, HttpStatusCode.NotFound);

                var alreadyAssigned = await _taskRepository.IsAssignedAsync(task!.Id, callerId);
                if (alreadyAssigned)
                    return CommonResponseFactory.CreateErrorResponse<string>("You're already assigned to this task.", HttpStatusCode.BadRequest);

                var gate = await _approvalGate.ExecuteOrRequestApprovalAsync(
                    ApprovalActionType.AssignTaskRequest,
                    nameof(TaskItem),
                    task.Id,
                    needsApproval: true,
                    requestedByUserId: callerId,
                    execute: () => Task.CompletedTask,
                    payload: new { UserId = callerId });

                if (gate.AlreadyPending)
                    return CommonResponseFactory.CreateSuccessResponse("You've already requested to be assigned to this task.", "Already pending.");

                return CommonResponseFactory.CreateSuccessResponse("Requested — an admin will need to approve it.", "Requested.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<List<TaskApprovalRequestDTO>>> GetMyPendingTaskApprovalsAsync(int callerId)
        {
            var response = new CommonResponse<List<TaskApprovalRequestDTO>>();

            try
            {
                var requests = await _taskApprovalRepository.GetPendingForApproverAsync(callerId);
                return CommonResponseFactory.CreateSuccessResponse(requests.Select(TaskApprovalRequestDTO.FromEntity).ToList(), "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<List<TaskApprovalRequestDTO>>> GetMySentTaskApprovalsAsync(int callerId)
        {
            var response = new CommonResponse<List<TaskApprovalRequestDTO>>();

            try
            {
                var requests = await _taskApprovalRepository.GetRequestedByAsync(callerId);
                return CommonResponseFactory.CreateSuccessResponse(requests.Select(TaskApprovalRequestDTO.FromEntity).ToList(), "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Pending UpdateTask requests on tasks the caller created — theirs to
        // approve or reject, whatever their role.
        public async Task<CommonResponse<List<ApprovalRequestDTO>>> GetPendingEditRequestsForCreatorAsync(int callerId)
        {
            var response = new CommonResponse<List<ApprovalRequestDTO>>();

            try
            {
                var requests = await _approvalRepository.GetPendingForTaskCreatorAsync(callerId);
                return CommonResponseFactory.CreateSuccessResponse(requests.Select(ApprovalRequestDTO.FromEntity).ToList(), "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        // Both edit and delete requests the caller has filed on tasks —
        // "Sent by me" shows both kinds, newest first.
        public async Task<CommonResponse<List<ApprovalRequestDTO>>> GetSentEditRequestsAsync(int callerId)
        {
            var response = new CommonResponse<List<ApprovalRequestDTO>>();

            try
            {
                var edits = await _approvalRepository.GetSentByAsync(callerId, ApprovalActionType.UpdateTask);
                var deletes = await _approvalRepository.GetSentByAsync(callerId, ApprovalActionType.DeleteTask);
                var requests = edits.Concat(deletes).OrderByDescending(r => r.CreatedAt).ToList();
                return CommonResponseFactory.CreateSuccessResponse(requests.Select(ApprovalRequestDTO.FromEntity).ToList(), "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        // Reuses ApprovalService's actual approve/reject logic (it doesn't
        // itself check who's calling) — only the authorization here differs
        // from the Admin queue: the task's creator may resolve this one too.
        // Covers both UpdateTask and DeleteTask requests — both are
        // creator-resolved the same way.
        public async Task<CommonResponse<ApprovalRequestDTO>> RespondToEditRequestAsync(int requestId, int callerId, bool callerIsAdmin, bool approve, string? note)
        {
            var response = new CommonResponse<ApprovalRequestDTO>();

            try
            {
                var request = await _approvalRepository.GetByIdAsync(requestId);
                if (request is null || (request.ActionType != ApprovalActionType.UpdateTask && request.ActionType != ApprovalActionType.DeleteTask))
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>("Request not found.", HttpStatusCode.NotFound);

                var task = await _taskRepository.GetByIdAsync(request.TargetEntityId);
                if (task is null)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>("Task not found.", HttpStatusCode.NotFound);

                if (!callerIsAdmin && task.CreatedByUserId != callerId)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>(
                        "Only this task's creator can respond to this request.", HttpStatusCode.Forbidden);

                var dto = new ResolveApprovalDto { Note = note };
                return approve
                    ? await _approvalService.ApproveAsync(requestId, callerId, dto)
                    : await _approvalService.RejectAsync(requestId, callerId, dto);
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // --- Duplicate requests: resolved by a Manager/Admin of the source task's project ---

        // Pending DuplicateTask requests the caller can resolve — every one
        // of them if they're an Admin, or just the ones on projects they
        // manage otherwise.
        public async Task<CommonResponse<List<ApprovalRequestDTO>>> GetPendingDuplicateRequestsForManagerAsync(int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<List<ApprovalRequestDTO>>();

            try
            {
                var requests = await _approvalRepository.GetPendingDuplicateRequestsForManagerAsync(callerId, callerIsAdmin);
                return CommonResponseFactory.CreateSuccessResponse(requests.Select(ApprovalRequestDTO.FromEntity).ToList(), "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<List<ApprovalRequestDTO>>> GetSentDuplicateRequestsAsync(int callerId)
        {
            var response = new CommonResponse<List<ApprovalRequestDTO>>();

            try
            {
                var requests = await _approvalRepository.GetSentByAsync(callerId, ApprovalActionType.DuplicateTask);
                return CommonResponseFactory.CreateSuccessResponse(requests.Select(ApprovalRequestDTO.FromEntity).ToList(), "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        // Same reuse of ApprovalService's approve/reject as RespondToEditRequestAsync —
        // only the authorization differs: a Manager (or Admin) of the source
        // task's project resolves this one, not that task's creator.
        public async Task<CommonResponse<ApprovalRequestDTO>> RespondToDuplicateRequestAsync(int requestId, int callerId, bool callerIsAdmin, bool approve, string? note)
        {
            var response = new CommonResponse<ApprovalRequestDTO>();

            try
            {
                var request = await _approvalRepository.GetByIdAsync(requestId);
                if (request is null || request.ActionType != ApprovalActionType.DuplicateTask)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>("Request not found.", HttpStatusCode.NotFound);

                var (_, project, error) = await LoadTaskAndProjectAsync(request.TargetEntityId);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>(error, HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckManager(project!, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>(access, HttpStatusCode.Forbidden);

                var dto = new ResolveApprovalDto { Note = note };
                return approve
                    ? await _approvalService.ApproveAsync(requestId, callerId, dto)
                    : await _approvalService.RejectAsync(requestId, callerId, dto);
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // --- Busy-user assignment requests: resolved by the target user, or an Admin ---

        // Every open request asking to add the caller to a task while
        // they're marked busy — their own "Waiting on you" queue.
        public async Task<CommonResponse<List<ApprovalRequestDTO>>> GetPendingBusyAssignmentsForMeAsync(int callerId)
        {
            var response = new CommonResponse<List<ApprovalRequestDTO>>();

            try
            {
                var requests = await _approvalRepository.GetPendingBusyAssignmentsForUserAsync(callerId);
                return CommonResponseFactory.CreateSuccessResponse(requests.Select(ApprovalRequestDTO.FromEntity).ToList(), "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<List<ApprovalRequestDTO>>> GetSentBusyAssignmentRequestsAsync(int callerId)
        {
            var response = new CommonResponse<List<ApprovalRequestDTO>>();

            try
            {
                var requests = await _approvalRepository.GetSentByAsync(callerId, ApprovalActionType.AssignBusyUser);
                return CommonResponseFactory.CreateSuccessResponse(requests.Select(ApprovalRequestDTO.FromEntity).ToList(), "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        // Reuses ApprovalService's actual approve/reject logic (it doesn't
        // itself check who's calling) — only the authorization here differs
        // from the Admin queue: the person being assigned may resolve this too.
        public async Task<CommonResponse<ApprovalRequestDTO>> RespondToBusyAssignmentAsync(int requestId, int callerId, bool callerIsAdmin, bool approve, string? note)
        {
            var response = new CommonResponse<ApprovalRequestDTO>();

            try
            {
                var request = await _approvalRepository.GetByIdAsync(requestId);
                if (request is null || request.ActionType != ApprovalActionType.AssignBusyUser)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>("Assignment request not found.", HttpStatusCode.NotFound);

                var targetUserId = ExtractPayloadUserId(request.PayloadJson);
                if (!callerIsAdmin && targetUserId != callerId)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>(
                        "Only the person being assigned can respond to this request.", HttpStatusCode.Forbidden);

                var dto = new ResolveApprovalDto { Note = note };
                return approve
                    ? await _approvalService.ApproveAsync(requestId, callerId, dto)
                    : await _approvalService.RejectAsync(requestId, callerId, dto);
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // --- To-do items: a separate, self-ticked checklist — no files, no
        // review, open to any project member the same way viewing the task is.

        public async Task<CommonResponse<TaskTodoItemDTO>> AddTodoAsync(int taskId, int callerId, bool callerIsAdmin, CreateTaskTodoItemDto dto)
        {
            var response = new CommonResponse<TaskTodoItemDTO>();

            try
            {
                var (task, project, error) = await LoadTaskAndProjectAsync(taskId);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskTodoItemDTO>(error, HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckMember(project!, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskTodoItemDTO>(access, HttpStatusCode.Forbidden);

                var text = (dto.Text ?? string.Empty).Trim();
                if (text.Length == 0)
                    return CommonResponseFactory.CreateErrorResponse<TaskTodoItemDTO>("A to-do needs some text.", HttpStatusCode.BadRequest);
                if (text.Length > 30)
                    return CommonResponseFactory.CreateErrorResponse<TaskTodoItemDTO>("Keep a to-do to 30 characters or fewer.", HttpStatusCode.BadRequest);

                var todo = new TaskTodoItem { TaskId = task!.Id, Text = text };
                await _taskRepository.AddTodoAsync(todo);

                return CommonResponseFactory.CreateSuccessResponse(TaskTodoItemDTO.FromEntity(todo), "Added.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<TaskTodoItemDTO>> ToggleTodoAsync(int taskId, int todoId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<TaskTodoItemDTO>();

            try
            {
                var todo = await _taskRepository.GetTodoAsync(todoId);
                if (todo is null || todo.TaskId != taskId)
                    return CommonResponseFactory.CreateErrorResponse<TaskTodoItemDTO>("To-do not found.", HttpStatusCode.NotFound);

                var (_, project, error) = await LoadTaskAndProjectAsync(taskId);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskTodoItemDTO>(error, HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckMember(project!, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<TaskTodoItemDTO>(access, HttpStatusCode.Forbidden);

                todo.IsDone = !todo.IsDone;
                await _taskRepository.UpdateTodoAsync(todo);

                return CommonResponseFactory.CreateSuccessResponse(TaskTodoItemDTO.FromEntity(todo), "Updated.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<string>> DeleteTodoAsync(int taskId, int todoId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<string>();

            try
            {
                var todo = await _taskRepository.GetTodoAsync(todoId);
                if (todo is null || todo.TaskId != taskId)
                    return CommonResponseFactory.CreateErrorResponse<string>("To-do not found.", HttpStatusCode.NotFound);

                var (_, project, error) = await LoadTaskAndProjectAsync(taskId);
                if (error is not null)
                    return CommonResponseFactory.CreateErrorResponse<string>(error, HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckMember(project!, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<string>(access, HttpStatusCode.Forbidden);

                await _taskRepository.RemoveTodoAsync(todo);

                return CommonResponseFactory.CreateSuccessResponse("To-do removed.", "Removed.");
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

        // An Admin can assign anyone regardless of busy status. Everyone
        // else gets split: available users go straight through, busy ones
        // need their own (or an Admin's) sign-off first — see
        // RequestBusyAssignmentsAsync.
        private async Task<(List<int> Immediate, List<int> Busy)> SplitByAvailabilityAsync(List<int> userIds, bool callerIsAdmin)
        {
            if (callerIsAdmin || userIds.Count == 0)
                return (userIds, new List<int>());

            var users = await _userRepository.GetByIdsAsync(userIds);
            var busyIds = users.Where(u => !u.IsAvailableForAssignment).Select(u => u.Id).ToHashSet();
            var immediate = userIds.Where(id => !busyIds.Contains(id)).ToList();
            var busy = userIds.Where(busyIds.Contains).ToList();
            return (immediate, busy);
        }

        // Files a generic ApprovalRequest per busy user directly (bypassing
        // IApprovalGate) because its "one pending request per target" guard
        // is keyed only on (TargetEntityType, TargetEntityId) — it would let
        // only the first of several busy assignees on the same task ever get
        // a request filed. HasPendingBusyAssignmentAsync does the equivalent
        // duplicate check, scoped per (task, user) instead of just per task.
        private async Task RequestBusyAssignmentsAsync(TaskItem task, List<int> busyUserIds, int callerId)
        {
            foreach (var userId in busyUserIds)
            {
                if (await _approvalRepository.HasPendingBusyAssignmentAsync(task.Id, userId))
                    continue;

                var request = new ApprovalRequest
                {
                    ActionType = ApprovalActionType.AssignBusyUser,
                    TargetEntityType = nameof(TaskItem),
                    TargetEntityId = task.Id,
                    RequestedByUserId = callerId,
                    PayloadJson = JsonSerializer.Serialize(new { UserId = userId }),
                    Status = ApprovalStatus.Pending
                };
                await _approvalRepository.CreateAsync(request);

                await _notifier.NotifyAsync(userId, NotificationType.ApprovalRequired,
                    $"You're marked busy — someone wants to assign you to '{task.Title}'. Approve or decline it.",
                    nameof(TaskItem), task.Id);
            }

            if (busyUserIds.Count > 0)
            {
                await _activityLogger.LogAsync(callerId, ActivityAction.ApprovalRequested,
                    $"Requested to assign {busyUserIds.Count} busy user(s) to '{task.Title}'.", task.ProjectId, task.Id);
            }
        }

        private static int? ExtractPayloadUserId(string? payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(payloadJson);
                return doc.RootElement.TryGetProperty("UserId", out var prop) ? prop.GetInt32() : null;
            }
            catch (JsonException)
            {
                return null;
            }
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

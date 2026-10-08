using System.Text.Json;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.Exceptions;
using TaskPulse.API.Repository.TaskRepo;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations.ApprovalHandlers
{
    // Approving a duplicate request is what actually creates the copy — the
    // source task named by the request (TargetEntityId) is only ever read,
    // never touched. Deliberately simpler than TaskService.DuplicateTaskAsync's
    // own immediate (Manager/Admin) path: assignees go on directly rather than
    // through the busy-user split, since a manager has already looked at the
    // request once before approving it.
    public class DuplicateTaskApprovalHandler : IApprovalActionHandler
    {
        private readonly TaskRepository _taskRepository;
        private readonly INotifier _notifier;

        public DuplicateTaskApprovalHandler(TaskRepository taskRepository, INotifier notifier)
        {
            _taskRepository = taskRepository;
            _notifier = notifier;
        }

        public ApprovalActionType ActionType => ApprovalActionType.DuplicateTask;

        public async Task ExecuteAsync(ApprovalRequest request)
        {
            var source = await _taskRepository.GetByIdAsync(request.TargetEntityId);
            if (source is null)
                throw new DataNotFound($"Task #{request.TargetEntityId} no longer exists.");

            if (string.IsNullOrWhiteSpace(request.PayloadJson))
                throw new DataNotFound("This request is missing the task details to duplicate.");

            var dto = JsonSerializer.Deserialize<CreateTaskDto>(request.PayloadJson)
                ?? throw new DataNotFound("This request's task details could not be read.");

            var task = new TaskItem
            {
                ProjectId = dto.ProjectId,
                Title = dto.Title,
                Description = dto.Description,
                Priority = dto.Priority,
                DueDate = dto.DueDate,
                EstimatedHours = dto.EstimatedHours,
                CreatedByUserId = request.RequestedByUserId,
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
                await _taskRepository.ReplaceAssignmentsAsync(task, dto.AssignedUserIds, UserRole.Manager);

                // ApprovalService already tells the requester once ExecuteAsync
                // returns — this only covers assignees who aren't that person.
                foreach (var userId in dto.AssignedUserIds.Distinct().Where(id => id != request.RequestedByUserId))
                {
                    await _notifier.NotifyAsync(userId, NotificationType.TaskAssigned,
                        $"You were assigned '{task.Title}'.", nameof(TaskItem), task.Id);
                }
            }
        }
    }
}

using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.Exceptions;
using TaskPulse.API.Repository.TaskRepo;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations.ApprovalHandlers
{
    // Approving a task-completion request closes the task. ApprovalService
    // already notifies whoever submitted it — this additionally tells every
    // other assignee, since a task can have more than one.
    public class CompleteTaskApprovalHandler : IApprovalActionHandler
    {
        private readonly TaskRepository _taskRepository;
        private readonly INotifier _notifier;

        public CompleteTaskApprovalHandler(TaskRepository taskRepository, INotifier notifier)
        {
            _taskRepository = taskRepository;
            _notifier = notifier;
        }

        public ApprovalActionType ActionType => ApprovalActionType.CompleteTask;

        public async Task ExecuteAsync(ApprovalRequest request)
        {
            var task = await _taskRepository.GetByIdAsync(request.TargetEntityId);
            if (task is null)
                throw new DataNotFound($"Task #{request.TargetEntityId} no longer exists.");

            task.Status = TaskItemStatus.Done;
            task.CompletedDate = DateTime.UtcNow;
            await _taskRepository.UpdateAsync(task);

            var otherAssigneeIds = task.Assignments
                .Select(a => a.UserId)
                .Distinct()
                .Where(userId => userId != request.RequestedByUserId);

            foreach (var userId in otherAssigneeIds)
            {
                await _notifier.NotifyAsync(
                    userId,
                    NotificationType.ApprovalResolved,
                    $"Task \"{task.Title}\" was approved and marked complete.",
                    nameof(TaskItem),
                    task.Id);
            }
        }
    }
}

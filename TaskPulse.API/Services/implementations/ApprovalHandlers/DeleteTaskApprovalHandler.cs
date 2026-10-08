using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.Exceptions;
using TaskPulse.API.Repository.TaskRepo;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations.ApprovalHandlers
{
    public class DeleteTaskApprovalHandler : IApprovalActionHandler
    {
        private readonly TaskRepository _taskRepository;

        public DeleteTaskApprovalHandler(TaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public ApprovalActionType ActionType => ApprovalActionType.DeleteTask;

        public async Task ExecuteAsync(ApprovalRequest request)
        {
            var task = await _taskRepository.GetByIdAsync(request.TargetEntityId);

            // The task can have been deleted some other way while the
            // request sat waiting — that's a stale request, not a crash.
            if (task is null)
                throw new DataNotFound($"Task #{request.TargetEntityId} no longer exists.");

            await _taskRepository.DeleteAsync(task);
        }
    }
}

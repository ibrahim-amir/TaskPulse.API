using System.Text.Json;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.Exceptions;
using TaskPulse.API.Repository.TaskRepo;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations.ApprovalHandlers
{
    public class AssignTaskRequestApprovalHandler : IApprovalActionHandler
    {
        private readonly TaskRepository _taskRepository;

        public AssignTaskRequestApprovalHandler(TaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public ApprovalActionType ActionType => ApprovalActionType.AssignTaskRequest;

        private class RequestPayload
        {
            public int UserId { get; set; }
        }

        public async Task ExecuteAsync(ApprovalRequest request)
        {
            var task = await _taskRepository.GetByIdAsync(request.TargetEntityId);
            if (task is null)
                throw new DataNotFound($"Task #{request.TargetEntityId} no longer exists.");

            var payload = JsonSerializer.Deserialize<RequestPayload>(request.PayloadJson ?? "{}");
            if (payload is null || payload.UserId == 0)
                throw new DataNotFound("This request no longer has a valid person to assign.");

            var currentAssigneeIds = await _taskRepository.GetAssigneeIdsAsync(task.Id);
            if (currentAssigneeIds.Contains(payload.UserId))
                return;

            var nextAssigneeIds = currentAssigneeIds.Append(payload.UserId).ToList();
            await _taskRepository.ReplaceAssignmentsAsync(task, nextAssigneeIds, UserRole.Admin);
        }
    }
}

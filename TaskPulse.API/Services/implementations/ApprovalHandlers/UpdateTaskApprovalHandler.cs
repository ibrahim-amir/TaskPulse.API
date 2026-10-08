using System.Text.Json;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.Exceptions;
using TaskPulse.API.Repository.TaskRepo;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations.ApprovalHandlers
{
    public class UpdateTaskApprovalHandler : IApprovalActionHandler
    {
        private readonly TaskRepository _taskRepository;

        public UpdateTaskApprovalHandler(TaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public ApprovalActionType ActionType => ApprovalActionType.UpdateTask;

        public async Task ExecuteAsync(ApprovalRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PayloadJson))
                throw new DataNotFound($"Approval #{request.Id} has no change to apply.");

            var dto = JsonSerializer.Deserialize<UpdateTaskDto>(request.PayloadJson)
                      ?? throw new DataNotFound($"Approval #{request.Id} has an unreadable change payload.");

            var task = await _taskRepository.GetByIdAsync(request.TargetEntityId);
            if (task is null)
                throw new DataNotFound($"Task #{request.TargetEntityId} no longer exists.");

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Status = dto.Status;
            task.Priority = dto.Priority;
            task.EstimatedHours = dto.EstimatedHours;
            task.ActualHours = dto.ActualHours;

            if (task.DueDate != dto.DueDate)
            {
                task.DueDate = dto.DueDate;
                task.DueSoonNotified = false;
                task.OverdueNotified = false;
            }

            await _taskRepository.UpdateAsync(task);
        }
    }
}

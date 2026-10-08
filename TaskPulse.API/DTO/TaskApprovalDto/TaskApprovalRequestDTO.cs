using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.TaskApprovalDto
{
    public class TaskApprovalRequestDTO
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int ProjectId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ApprovalStatus Status { get; set; }
        public int RequestedByUserId { get; set; }
        public string RequestedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public List<TaskApprovalResponseDTO> Responses { get; set; } = new();

        public static TaskApprovalRequestDTO FromEntity(TaskApprovalRequest request)
        {
            return new TaskApprovalRequestDTO
            {
                Id = request.Id,
                TaskId = request.TaskId,
                ProjectId = request.Task?.ProjectId ?? 0,
                TaskTitle = request.Task?.Title ?? string.Empty,
                Description = request.Description,
                Status = request.Status,
                RequestedByUserId = request.RequestedByUserId,
                RequestedByUserName = request.RequestedByUser?.Name ?? string.Empty,
                CreatedAt = request.CreatedAt,
                ResolvedDate = request.ResolvedDate,
                Responses = request.Responses.Select(TaskApprovalResponseDTO.FromEntity).ToList()
            };
        }
    }
}

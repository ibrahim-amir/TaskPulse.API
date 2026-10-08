using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.TaskApprovalDto
{
    public class TaskApprovalResponseDTO
    {
        public int Id { get; set; }
        public int ApproverUserId { get; set; }
        public string ApproverUserName { get; set; } = string.Empty;
        public ApprovalStatus Status { get; set; }
        public DateTime? RespondedDate { get; set; }
        public string? Note { get; set; }

        public static TaskApprovalResponseDTO FromEntity(TaskApprovalResponse response)
        {
            return new TaskApprovalResponseDTO
            {
                Id = response.Id,
                ApproverUserId = response.ApproverUserId,
                ApproverUserName = response.ApproverUser?.Name ?? string.Empty,
                Status = response.Status,
                RespondedDate = response.RespondedDate,
                Note = response.Note
            };
        }
    }
}

using TaskPulse.API.Entities.BaseEntityClass;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Entities
{
    // One tagged approver's decision on a TaskApprovalRequest. The parent
    // request is only Approved once every one of these is Approved.
    public class TaskApprovalResponse : BasicEntityClass
    {
        public int TaskApprovalRequestId { get; set; }
        public TaskApprovalRequest TaskApprovalRequest { get; set; } = null!;

        public int ApproverUserId { get; set; }
        public User ApproverUser { get; set; } = null!;

        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
        public DateTime? RespondedDate { get; set; }
        public string? Note { get; set; }
    }
}

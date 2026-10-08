using TaskPulse.API.Entities.BaseEntityClass;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Entities
{
    // A task submission that needs sign-off from specific tagged people before
    // it can be finalized. Distinct from the generic ApprovalRequest gate
    // (resolved by any Admin, action auto-executes on approval) — here the
    // requester picks exactly who must approve, every one of them has to say
    // yes, and finishing the task is still a separate, explicit Submit step.
    public class TaskApprovalRequest : BasicEntityClass
    {
        public int TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;

        public int RequestedByUserId { get; set; }
        public User RequestedByUser { get; set; } = null!;

        public string? Description { get; set; }

        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
        public DateTime? ResolvedDate { get; set; }

        public ICollection<TaskApprovalResponse> Responses { get; set; } = new List<TaskApprovalResponse>();
    }
}

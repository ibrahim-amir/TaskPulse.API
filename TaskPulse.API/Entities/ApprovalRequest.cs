using TaskPulse.API.Entities.BaseEntityClass;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Entities
{
    public class ApprovalRequest : BasicEntityClass
    {
        public ApprovalActionType ActionType { get; set; }

        public string TargetEntityType { get; set; } = string.Empty;
        public int TargetEntityId { get; set; }

        public int RequestedByUserId { get; set; }
        public User RequestedByUser { get; set; } = null!;
        public string? PayloadJson { get; set; }

        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        public DateTime? ResolvedDate { get; set; }
        public int? ResolvedByUserId { get; set; }
        public User? ResolvedByUser { get; set; }
        public string? ResolutionNote { get; set; }
    }
}

using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.ApprovalDto;

public class ApprovalRequestDTO
{
    public int Id { get; set; }
    public ApprovalActionType ActionType { get; set; }
    public string TargetEntityType { get; set; } = string.Empty;
    public int TargetEntityId { get; set; }
    public int RequestedByUserId { get; set; }
    public string RequestedByUserName { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public ApprovalStatus Status { get; set; }
    public DateTime RequestedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public int? ResolvedByUserId { get; set; }
    public string? ResolutionNote { get; set; }

    public static ApprovalRequestDTO FromEntity(ApprovalRequest request)
    {
        return new ApprovalRequestDTO
        {
            Id = request.Id,
            ActionType = request.ActionType,
            TargetEntityType = request.TargetEntityType,
            TargetEntityId = request.TargetEntityId,
            RequestedByUserId = request.RequestedByUserId,
            RequestedByUserName = request.RequestedByUser?.Name ?? string.Empty,
            PayloadJson = request.PayloadJson,
            Status = request.Status,
            RequestedDate = request.CreatedAt,
            ResolvedDate = request.ResolvedDate,
            ResolvedByUserId = request.ResolvedByUserId,
            ResolutionNote = request.ResolutionNote
        };
    }
}

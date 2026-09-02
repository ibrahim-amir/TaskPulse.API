using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.ApprovalDto;

// Query string for GET /api/approvals. Defaults to the pending queue, since
// that's the only list that needs acting on.
public class ApprovalQueryDto : PagedRequest
{
    public ApprovalStatus? Status { get; set; } = ApprovalStatus.Pending;
}

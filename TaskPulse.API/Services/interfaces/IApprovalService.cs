using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.ApprovalDto;
using TaskPulse.API.DTO.PagingDto;

namespace TaskPulse.API.Services.interfaces
{
    public interface IApprovalService
    {
        Task<CommonResponse<PagedResult<ApprovalRequestDTO>>> GetRequestsAsync(ApprovalQueryDto query);

        Task<CommonResponse<ApprovalRequestDTO>> GetByIdAsync(int id);

        Task<CommonResponse<ApprovalRequestDTO>> ApproveAsync(int id, int adminUserId, ResolveApprovalDto dto);

        Task<CommonResponse<ApprovalRequestDTO>> RejectAsync(int id, int adminUserId, ResolveApprovalDto dto);
    }
}

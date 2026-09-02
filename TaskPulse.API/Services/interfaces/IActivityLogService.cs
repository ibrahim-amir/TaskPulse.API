using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.ActivityLogDto;
using TaskPulse.API.DTO.PagingDto;

namespace TaskPulse.API.Services.interfaces
{
    // The read side of the audit trail. IActivityLogger writes it.
    public interface IActivityLogService
    {
        Task<CommonResponse<PagedResult<ActivityLogDTO>>> GetLogsAsync(ActivityLogQueryDto query, int callerId, bool callerIsAdmin);
    }
}

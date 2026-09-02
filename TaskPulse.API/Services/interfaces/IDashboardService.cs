using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.DashboardDto;

namespace TaskPulse.API.Services.interfaces
{
    // Everything is scoped to what the caller can see: the Admin gets the whole
    // system, everyone else only the projects they're a member of.
    public interface IDashboardService
    {
        Task<CommonResponse<DashboardSummaryDto>> GetSummaryAsync(int callerId, bool callerIsAdmin);

        Task<CommonResponse<ProjectDashboardDto>> GetProjectDashboardAsync(int projectId, int callerId, bool callerIsAdmin);

        Task<CommonResponse<ProgressReportDto>> GetProgressAsync(int callerId, bool callerIsAdmin);
    }
}

using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.AttendanceDto;

namespace TaskPulse.API.Services.interfaces
{
    // Attendance is read back out of the activity log rather than tracked
    // separately: access tokens last 24 hours, so a login is a day present.
    public interface IAttendanceService
    {
        Task<CommonResponse<AttendanceSummaryDto>> GetAttendanceAsync(int userId, AttendanceQueryDto query);
    }
}

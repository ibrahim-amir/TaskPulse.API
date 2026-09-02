using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.Controllers.Infrastructure;
using TaskPulse.API.DTO.AttendanceDto;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardController : BaseApiController
    {
        private readonly IDashboardService _dashboardService;
        private readonly IAttendanceService _attendanceService;

        public DashboardController(IDashboardService dashboardService, IAttendanceService attendanceService)
        {
            _dashboardService = dashboardService;
            _attendanceService = attendanceService;
        }

        // Headline counters plus the two required charts, scoped to what the
        // caller can see.
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            return Respond(await _dashboardService.GetSummaryAsync(User.GetUserId(), User.IsAdmin()));
        }

        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetProjectDashboard(int projectId)
        {
            return Respond(await _dashboardService.GetProjectDashboardAsync(projectId, User.GetUserId(), User.IsAdmin()));
        }

        // Estimated versus actual hours, and deadline health.
        [HttpGet("progress")]
        public async Task<IActionResult> GetProgress()
        {
            return Respond(await _dashboardService.GetProgressAsync(User.GetUserId(), User.IsAdmin()));
        }

        // The caller's own attendance. Someone else's goes through
        // /api/users/{id}/attendance, which is Admin-or-self.
        [HttpGet("attendance")]
        public async Task<IActionResult> GetMyAttendance([FromQuery] AttendanceQueryDto query)
        {
            return Respond(await _attendanceService.GetAttendanceAsync(User.GetUserId(), query));
        }
    }
}

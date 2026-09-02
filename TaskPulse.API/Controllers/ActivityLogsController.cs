using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.Controllers.Infrastructure;
using TaskPulse.API.DTO.ActivityLogDto;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Controllers
{
    // The audit trail. Not role-gated at the route: the service scopes it
    // instead, so the Admin sees everything and everyone else sees their own
    // projects plus their own entries.
    [Route("api/activity-logs")]
    [ApiController]
    [Authorize]
    public class ActivityLogsController : BaseApiController
    {
        private readonly IActivityLogService _activityLogService;

        public ActivityLogsController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs([FromQuery] ActivityLogQueryDto query)
        {
            return Respond(await _activityLogService.GetLogsAsync(query, User.GetUserId(), User.IsAdmin()));
        }
    }
}

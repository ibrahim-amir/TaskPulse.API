using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.Controllers.Infrastructure;
using TaskPulse.API.DTO.AttendanceDto;
using TaskPulse.API.DTO.NotificationDto;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class usersController : BaseApiController
    {
        private readonly IUserServices _userservice;
        private readonly IAttendanceService _attendanceService;
        private readonly INotificationService _notificationService;
        private readonly IProjectService _projectService;

        public usersController(
            IUserServices userservice,
            IAttendanceService attendanceService,
            INotificationService notificationService,
            IProjectService projectService)
        {
            _userservice = userservice;
            _attendanceService = attendanceService;
            _notificationService = notificationService;
            _projectService = projectService;
        }

        // Read-only directory — any authenticated user can look someone up.
        // UserDTO carries no sensitive fields (no password/token data), so
        // this is safe to open beyond Admin. Role/status/disable changes
        // below stay Admin-only.
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] UserQueryDto query)
        {
            return Respond(await _userservice.GetUsersAsync(query));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            return Respond(await _userservice.GetUserById(id));
        }

        // The projects this user belongs to — for their profile page.
        [HttpGet("{id}/projects")]
        public async Task<IActionResult> GetUserProjects(int id)
        {
            return Respond(await _projectService.GetProjectsForUserAsync(id));
        }

        // Sends a manual, user-composed notification to another user — the
        // "notify" action from a profile page. Open to any authenticated user.
        [HttpPost("{id}/notifications")]
        public async Task<IActionResult> SendNotification(int id, [FromForm] SendNotificationDto dto)
        {
            dto.RecipientUserId = id;
            return Respond(await _notificationService.SendAsync(dto, User.GetUserId()));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProfile(int id, UpdateUserProfileDto dto)
        {
            if (!User.CanAccessUser(id))
                return Forbidden("You can only update your own account.");

            //The id is the route's, never the body's.
            dto.Id = id;
            return Respond(await _userservice.UpdateUserProfileAsync(dto));
        }

        // Multipart, so the file comes in as a form field rather than JSON —
        // same self-or-admin rule as the rest of the profile.
        [HttpPost("{id}/avatar")]
        public async Task<IActionResult> UploadAvatar(int id, IFormFile file)
        {
            if (!User.CanAccessUser(id))
                return Forbidden("You can only update your own avatar.");

            return Respond(await _userservice.UpdateAvatarAsync(id, file));
        }

        [HttpPut("{id}/role")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> UpdateRole(int id, UpdateUserRoleDto dto)
        {
            dto.Id = id;
            return Respond(await _userservice.UpdateUserRoleAsync(dto));
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> UpdateStatus(int id, UpdateUserStatusDto dto)
        {
            dto.Id = id;
            return Respond(await _userservice.SetUserStatusAsync(dto));
        }

        // A real row delete — only succeeds when the account has no history.
        // For an account with real activity, disable it instead via
        // PUT /{id}/status, which preserves their tasks, comments and
        // activity history.
        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> DeleteUser(int id)
        {
            return Respond(await _userservice.DeleteUserAsync(id, User.GetUserId()));
        }

        [HttpGet("{id}/attendance")]
        public async Task<IActionResult> GetAttendance(int id, [FromQuery] AttendanceQueryDto query)
        {
            if (!User.CanAccessUser(id))
                return Forbidden("You can only view your own attendance.");

            return Respond(await _attendanceService.GetAttendanceAsync(id, query));
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.Controllers.Infrastructure;
using TaskPulse.API.DTO.NotificationDto;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Controllers
{
    // Always the caller's own notifications — the user id comes from the token,
    // so there's no route or query parameter to tamper with.
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationsController : BaseApiController
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMine([FromQuery] NotificationQueryDto query)
        {
            return Respond(await _notificationService.GetMineAsync(query, User.GetUserId()));
        }

        // For the badge on the bell icon.
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            return Respond(await _notificationService.GetUnreadCountAsync(User.GetUserId()));
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            return Respond(await _notificationService.MarkReadAsync(id, User.GetUserId()));
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            return Respond(await _notificationService.MarkAllReadAsync(User.GetUserId()));
        }
    }
}

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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            return Respond(await _notificationService.DeleteAsync(id, User.GetUserId()));
        }

        // Clears the caller's entire notification list.
        [HttpDelete]
        public async Task<IActionResult> ClearAll()
        {
            return Respond(await _notificationService.ClearAllAsync(User.GetUserId()));
        }

        // Both halves of a DirectMessage exchange with one other user, oldest
        // first — the mirror copy SendAsync writes on the sender's own side is
        // what makes this possible without a separate Message table.
        [HttpGet("conversation/{otherUserId}")]
        public async Task<IActionResult> GetConversation(int otherUserId)
        {
            return Respond(await _notificationService.GetConversationAsync(User.GetUserId(), otherUserId));
        }

        // One row per person the caller has exchanged messages with — the
        // chat-list sidebar.
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            return Respond(await _notificationService.GetConversationsAsync(User.GetUserId()));
        }

        // Clears only the caller's own copy of this conversation — the other
        // person still sees theirs until they clear it too.
        [HttpDelete("conversation/{otherUserId}")]
        public async Task<IActionResult> ClearConversation(int otherUserId)
        {
            return Respond(await _notificationService.ClearConversationAsync(User.GetUserId(), otherUserId));
        }
    }
}

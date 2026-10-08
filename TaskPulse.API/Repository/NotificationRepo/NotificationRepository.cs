using Microsoft.EntityFrameworkCore;
using TaskPulse.API.Data;
using TaskPulse.API.DTO.NotificationDto;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.PagingHelper;
using TaskPulse.API.Repository.Infrastructure;

namespace TaskPulse.API.Repository.NotificationRepo
{
    public class NotificationRepository : BaseRepositoryClass
    {
        public NotificationRepository(AppDbContext db) : base(db) { }

        public async Task CreateAsync(Notification notification)
        {
            await _db.Notifications.AddAsync(notification);
            await _db.SaveChangesAsync();
        }

        // Every Admin gets told when something needs approving. Single-org app,
        // so this is normally one account.
        public async Task<List<int>> GetActiveAdminIdsAsync()
        {
            return await _db.Users
                .Where(u => u.Role == UserRole.Admin && !u.IsDisabled)
                .Select(u => u.Id)
                .ToListAsync();
        }

        // Newest first by default — a notification list is read top-down.
        public async Task<PagedResult<Notification>> GetPagedForUserAsync(int userId, NotificationQueryDto query)
        {
            var notifications = _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId);

            if (query.IsRead.HasValue)
                notifications = notifications.Where(n => n.IsRead == query.IsRead.Value);

            return await notifications
                .ApplySorting(query.SortBy ?? nameof(Notification.Id), query.SortOrder ?? "desc")
                .ToPagedResultAsync(query);
        }

        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<int> CountUnreadAsync(int userId)
        {
            return await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkReadAsync(Notification notification)
        {
            notification.IsRead = true;
            _db.Notifications.Update(notification);
            await _db.SaveChangesAsync();
        }

        // One statement instead of loading every row just to flip a flag.
        public async Task<int> MarkAllReadAsync(int userId)
        {
            return await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(setter => setter.SetProperty(n => n.IsRead, true));
        }

        public async Task DeleteAsync(Notification notification)
        {
            _db.Notifications.Remove(notification);
            await _db.SaveChangesAsync();
        }

        public async Task<int> DeleteAllForUserAsync(int userId)
        {
            return await _db.Notifications.Where(n => n.UserId == userId).ExecuteDeleteAsync();
        }

        // Only the caller's own rows for this conversation — the other
        // person's copies (their own UserId-scoped rows) are untouched, so
        // clearing your view doesn't clear theirs.
        public async Task<int> DeleteConversationAsync(int userId, int otherUserId)
        {
            return await _db.Notifications
                .Where(n => n.UserId == userId
                         && n.Type == NotificationType.DirectMessage
                         && n.RelatedEntityType == "User"
                         && n.RelatedEntityId == otherUserId)
                .ExecuteDeleteAsync();
        }

        // Every DirectMessage row the caller owns that's about this one other
        // person — thanks to SendAsync's mirror copy, that's both halves of
        // the exchange, oldest first so it reads top-down like a chat.
        public async Task<List<Notification>> GetConversationAsync(int userId, int otherUserId)
        {
            return await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId
                         && n.Type == NotificationType.DirectMessage
                         && n.RelatedEntityType == "User"
                         && n.RelatedEntityId == otherUserId)
                .OrderBy(n => n.Id)
                .ToListAsync();
        }

        // One row per distinct counterparty, latest message + unread count —
        // the chat-app-style conversation list.
        public async Task<List<ConversationSummaryDto>> GetConversationSummariesAsync(int userId)
        {
            var grouped = await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId && n.Type == NotificationType.DirectMessage && n.RelatedEntityId != null)
                .GroupBy(n => n.RelatedEntityId!.Value)
                .Select(g => new
                {
                    OtherUserId = g.Key,
                    LastMessage = g.OrderByDescending(n => n.Id).First().Message,
                    LastMessageAt = g.OrderByDescending(n => n.Id).First().CreatedAt,
                    UnreadCount = g.Count(n => !n.IsOutgoing && !n.IsRead)
                })
                .ToListAsync();

            var otherUserIds = grouped.Select(g => g.OtherUserId).ToList();
            var users = await _db.Users
                .Where(u => otherUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => new { u.Name, u.profileImageUrl });

            return grouped
                .Select(g => new ConversationSummaryDto
                {
                    OtherUserId = g.OtherUserId,
                    OtherUserName = users.TryGetValue(g.OtherUserId, out var user) ? user.Name : "Unknown",
                    OtherUserProfileImageUrl = users.TryGetValue(g.OtherUserId, out var u2) ? u2.profileImageUrl : null,
                    LastMessage = g.LastMessage,
                    LastMessageAt = g.LastMessageAt,
                    UnreadCount = g.UnreadCount
                })
                .OrderByDescending(c => c.LastMessageAt)
                .ToList();
        }
    }
}

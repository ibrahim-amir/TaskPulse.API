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
    }
}

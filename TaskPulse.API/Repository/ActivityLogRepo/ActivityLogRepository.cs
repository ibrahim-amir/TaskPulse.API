using Microsoft.EntityFrameworkCore;
using TaskPulse.API.Data;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.EntityFieldNameEnum;
using TaskPulse.API.Repository.Infrastructure;

namespace TaskPulse.API.Repository.ActivityLogRepo
{
    public class ActivityLogRepository : BaseRepositoryClass
    {
        public ActivityLogRepository(AppDbContext db) : base(db) { }

        public async Task CreateAsync(ActivityLog log)
        {
            await _db.ActivityLogs.AddAsync(log);
            await _db.SaveChangesAsync();
        }

        // Every login timestamp in the range. Attendance groups these by date,
        // so a user logging in three times in one day is still one day present.
        public async Task<List<DateTime>> GetLoginTimesAsync(int userId, DateTime fromUtc, DateTime toUtc)
        {
            var loginAction = ActivityAction.Login.ToText();

            return await _db.ActivityLogs
                .AsNoTracking()
                .Where(a => a.UserId == userId
                         && a.Action == loginAction
                         && a.CreatedAt >= fromUtc
                         && a.CreatedAt <= toUtc)
                .Select(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}

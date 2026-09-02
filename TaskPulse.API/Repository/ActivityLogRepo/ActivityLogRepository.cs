using Microsoft.EntityFrameworkCore;
using TaskPulse.API.Data;
using TaskPulse.API.DTO.ActivityLogDto;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.EntityFieldNameEnum;
using TaskPulse.API.Helper.PagingHelper;
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

        // visibleProjectIds null means no scoping — the Admin sees everything.
        // Anyone else only sees entries for their own projects, plus their own
        // actions that aren't tied to a project at all.
        public async Task<PagedResult<ActivityLog>> GetPagedAsync(ActivityLogQueryDto query, List<int>? visibleProjectIds, int callerId)
        {
            var logs = _db.ActivityLogs
                .AsNoTracking()
                .Include(a => a.User)
                .AsQueryable();

            if (visibleProjectIds is not null)
            {
                logs = logs.Where(a => (a.ProjectId != null && visibleProjectIds.Contains(a.ProjectId.Value))
                                    || (a.ProjectId == null && a.UserId == callerId));
            }

            if (query.UserId.HasValue)
                logs = logs.Where(a => a.UserId == query.UserId.Value);

            if (query.ProjectId.HasValue)
                logs = logs.Where(a => a.ProjectId == query.ProjectId.Value);

            if (query.TaskId.HasValue)
                logs = logs.Where(a => a.TaskId == query.TaskId.Value);

            if (query.From.HasValue)
                logs = logs.Where(a => a.CreatedAt >= query.From.Value);

            if (query.To.HasValue)
                logs = logs.Where(a => a.CreatedAt <= query.To.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                logs = logs.Where(a => a.Action.Contains(search)
                                    || (a.Details != null && a.Details.Contains(search)));
            }

            return await logs
                .ApplySorting(query.SortBy ?? nameof(ActivityLog.Id), query.SortOrder ?? "desc")
                .ToPagedResultAsync(query);
        }
    }
}

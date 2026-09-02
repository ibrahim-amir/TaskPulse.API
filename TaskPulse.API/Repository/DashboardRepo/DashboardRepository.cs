using Microsoft.EntityFrameworkCore;
using TaskPulse.API.Data;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Repository.Infrastructure;

namespace TaskPulse.API.Repository.DashboardRepo
{
    // Read-only aggregation for the dashboard and reports. Kept apart from the
    // entity repositories because nothing here loads or saves an entity — it
    // counts and groups, and every method returns a shape the charts want.
    //
    // visibleProjectIds null means no scoping — the Admin. Anyone else passes
    // the projects they're a member of.
    public class DashboardRepository : BaseRepositoryClass
    {
        public DashboardRepository(AppDbContext db) : base(db) { }

        private IQueryable<TaskItem> ScopedTasks(List<int>? visibleProjectIds)
        {
            var tasks = _db.Tasks.AsNoTracking();

            return visibleProjectIds is null
                ? tasks
                : tasks.Where(t => visibleProjectIds.Contains(t.ProjectId));
        }

        public async Task<int> CountProjectsAsync(List<int>? visibleProjectIds)
        {
            var projects = _db.Projects.AsNoTracking();

            if (visibleProjectIds is not null)
                projects = projects.Where(p => visibleProjectIds.Contains(p.Id));

            return await projects.CountAsync();
        }

        public async Task<int> CountTasksAsync(List<int>? visibleProjectIds, int? projectId = null)
        {
            var tasks = ScopedTasks(visibleProjectIds);

            if (projectId.HasValue)
                tasks = tasks.Where(t => t.ProjectId == projectId.Value);

            return await tasks.CountAsync();
        }

        public async Task<int> CountTasksByStatusAsync(List<int>? visibleProjectIds, TaskItemStatus status, int? projectId = null)
        {
            var tasks = ScopedTasks(visibleProjectIds).Where(t => t.Status == status);

            if (projectId.HasValue)
                tasks = tasks.Where(t => t.ProjectId == projectId.Value);

            return await tasks.CountAsync();
        }

        public async Task<int> CountOverdueTasksAsync(List<int>? visibleProjectIds, int? projectId = null)
        {
            var now = DateTime.UtcNow;
            var tasks = ScopedTasks(visibleProjectIds)
                .Where(t => t.DueDate != null && t.DueDate < now && t.Status != TaskItemStatus.Done);

            if (projectId.HasValue)
                tasks = tasks.Where(t => t.ProjectId == projectId.Value);

            return await tasks.CountAsync();
        }

        // Open work assigned to one person — the "what's on my plate" tile.
        public async Task<int> CountOpenTasksForUserAsync(int userId)
        {
            return await _db.Tasks
                .AsNoTracking()
                .CountAsync(t => t.Status != TaskItemStatus.Done && t.Assignments.Any(a => a.UserId == userId));
        }

        public async Task<Dictionary<TaskItemStatus, int>> GroupTasksByStatusAsync(List<int>? visibleProjectIds, int? projectId = null)
        {
            var tasks = ScopedTasks(visibleProjectIds);

            if (projectId.HasValue)
                tasks = tasks.Where(t => t.ProjectId == projectId.Value);

            return await tasks
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        public async Task<Dictionary<PriorityLevel, int>> GroupTasksByPriorityAsync(List<int>? visibleProjectIds, int? projectId = null)
        {
            var tasks = ScopedTasks(visibleProjectIds);

            if (projectId.HasValue)
                tasks = tasks.Where(t => t.ProjectId == projectId.Value);

            return await tasks
                .GroupBy(t => t.Priority)
                .Select(g => new { Priority = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Priority, x => x.Count);
        }

        // Required-file progress across a whole project.
        public async Task<(int Required, int Uploaded)> CountChecklistProgressAsync(int projectId)
        {
            var subtasks = _db.TaskSubtasks
                .AsNoTracking()
                .Where(s => s.Task.ProjectId == projectId);

            var required = await subtasks.CountAsync();
            var uploaded = await subtasks.CountAsync(s => s.IsUploaded);

            return (required, uploaded);
        }

        public async Task<(decimal Estimated, decimal Actual)> SumHoursAsync(List<int>? visibleProjectIds, int? projectId = null)
        {
            var tasks = ScopedTasks(visibleProjectIds);

            if (projectId.HasValue)
                tasks = tasks.Where(t => t.ProjectId == projectId.Value);

            var estimated = await tasks.SumAsync(t => (decimal?)t.EstimatedHours) ?? 0m;
            var actual = await tasks.SumAsync(t => (decimal?)t.ActualHours) ?? 0m;

            return (estimated, actual);
        }

        // Open tasks bucketed by deadline. Done in one pass so the four numbers
        // always agree with each other.
        public async Task<(int OnTrack, int AtRisk, int Late, int NoDueDate)> CountDeadlineHealthAsync(List<int>? visibleProjectIds)
        {
            var now = DateTime.UtcNow;
            var riskCutoff = now.AddDays(3);

            var openTasks = ScopedTasks(visibleProjectIds).Where(t => t.Status != TaskItemStatus.Done);

            var noDueDate = await openTasks.CountAsync(t => t.DueDate == null);
            var late = await openTasks.CountAsync(t => t.DueDate != null && t.DueDate < now);
            var atRisk = await openTasks.CountAsync(t => t.DueDate != null && t.DueDate >= now && t.DueDate <= riskCutoff);
            var onTrack = await openTasks.CountAsync(t => t.DueDate != null && t.DueDate > riskCutoff);

            return (onTrack, atRisk, late, noDueDate);
        }

        public async Task<List<Project>> GetProjectsAsync(List<int>? visibleProjectIds)
        {
            var projects = _db.Projects.AsNoTracking();

            if (visibleProjectIds is not null)
                projects = projects.Where(p => visibleProjectIds.Contains(p.Id));

            return await projects.ToListAsync();
        }

        // One row per member of the project, counting the work assigned to them.
        public async Task<List<UserTaskTotals>> GetUserTotalsForProjectAsync(int projectId)
        {
            var now = DateTime.UtcNow;

            return await _db.ProjectMembers
                .AsNoTracking()
                .Where(m => m.ProjectId == projectId)
                .Select(m => new UserTaskTotals
                {
                    UserId = m.UserId,
                    UserName = m.User.Name,
                    AssignedTaskCount = _db.Tasks.Count(t => t.ProjectId == projectId && t.Assignments.Any(a => a.UserId == m.UserId)),
                    CompletedTaskCount = _db.Tasks.Count(t => t.ProjectId == projectId && t.Status == TaskItemStatus.Done && t.Assignments.Any(a => a.UserId == m.UserId)),
                    OverdueTaskCount = _db.Tasks.Count(t => t.ProjectId == projectId && t.DueDate != null && t.DueDate < now && t.Status != TaskItemStatus.Done && t.Assignments.Any(a => a.UserId == m.UserId)),
                    EstimatedHours = _db.Tasks.Where(t => t.ProjectId == projectId && t.Assignments.Any(a => a.UserId == m.UserId)).Sum(t => (decimal?)t.EstimatedHours) ?? 0m,
                    ActualHours = _db.Tasks.Where(t => t.ProjectId == projectId && t.Assignments.Any(a => a.UserId == m.UserId)).Sum(t => (decimal?)t.ActualHours) ?? 0m
                })
                .ToListAsync();
        }

        public async Task<int> CountPendingApprovalsAsync()
        {
            return await _db.ApprovalRequests.CountAsync(a => a.Status == ApprovalStatus.Pending);
        }
    }

    // Flat carrier for the performance query above, mapped to
    // UserPerformanceDto in the service.
    public class UserTaskTotals
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int AssignedTaskCount { get; set; }
        public int CompletedTaskCount { get; set; }
        public int OverdueTaskCount { get; set; }
        public decimal EstimatedHours { get; set; }
        public decimal ActualHours { get; set; }
    }
}

using Microsoft.EntityFrameworkCore;
using TaskPulse.API.Data;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.BackgroundServices
{
    // Nothing else in the app notices time passing: a due date arrives whether
    // or not anyone makes a request. This wakes up on a timer and sends the
    // "due tomorrow" and "deadline passed, still not submitted" notifications.
    //
    // Interval and the due-soon window come from the DeadlineMonitor block in
    // appsettings. It's a singleton, so it opens its own scope per pass rather
    // than holding a DbContext for the lifetime of the app.
    public class TaskDeadlineMonitor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TaskDeadlineMonitor> _logger;

        public TaskDeadlineMonitor(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<TaskDeadlineMonitor> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(
                int.TryParse(_configuration["DeadlineMonitor:CheckIntervalMinutes"], out var minutes) ? minutes : 60);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunPassAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // A failed pass must not kill the loop — the next one may
                    // well succeed, and the app shouldn't lose deadline
                    // notifications for good because of one bad query.
                    _logger.LogError(ex, "Deadline monitor pass failed.");
                }

                await Task.Delay(interval, stoppingToken);
            }
        }

        private async Task RunPassAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notifier = scope.ServiceProvider.GetRequiredService<INotifier>();

            var dueSoonHours = int.TryParse(_configuration["DeadlineMonitor:DueSoonHours"], out var hours) ? hours : 24;
            var now = DateTime.UtcNow;
            var dueSoonCutoff = now.AddHours(dueSoonHours);

            // Anything open with a deadline that's either close or already gone,
            // and that hasn't already been reported.
            var tasks = await db.Tasks
                .Include(t => t.Assignments)
                .Where(t => t.Status != TaskItemStatus.Done
                         && t.DueDate != null
                         && t.DueDate <= dueSoonCutoff
                         && (!t.DueSoonNotified || !t.OverdueNotified))
                .ToListAsync(stoppingToken);

            foreach (var task in tasks)
            {
                var overdue = task.DueDate < now;

                if (overdue && !task.OverdueNotified)
                {
                    await NotifyEveryoneAsync(db, notifier, task,
                        NotificationType.TaskDeadlineMissed,
                        $"'{task.Title}' passed its deadline and hasn't been submitted.");

                    task.OverdueNotified = true;
                    task.DueSoonNotified = true;
                }
                else if (!overdue && !task.DueSoonNotified)
                {
                    await NotifyEveryoneAsync(db, notifier, task,
                        NotificationType.TaskDueSoon,
                        $"'{task.Title}' is due {task.DueDate:g}.");

                    task.DueSoonNotified = true;
                }
            }

            if (tasks.Count > 0)
                await db.SaveChangesAsync(stoppingToken);
        }

        // Whoever is assigned, plus the project's managers so nothing slips by
        // unnoticed on an unassigned task.
        private static async Task NotifyEveryoneAsync(AppDbContext db, INotifier notifier, TaskItem task, NotificationType type, string message)
        {
            var recipientIds = task.Assignments.Select(a => a.UserId).ToList();

            var managerIds = await db.ProjectMembers
                .Where(m => m.ProjectId == task.ProjectId && m.Role == UserRole.Manager)
                .Select(m => m.UserId)
                .ToListAsync();

            foreach (var userId in recipientIds.Concat(managerIds).Distinct())
                await notifier.NotifyAsync(userId, type, message, nameof(TaskItem), task.Id);
        }
    }
}

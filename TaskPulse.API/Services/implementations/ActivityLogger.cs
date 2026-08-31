using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.EntityFieldNameEnum;
using TaskPulse.API.Repository.ActivityLogRepo;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    public class ActivityLogger : IActivityLogger
    {
        private readonly ActivityLogRepository _activityLogRepository;

        public ActivityLogger(ActivityLogRepository activityLogRepository)
        {
            _activityLogRepository = activityLogRepository;
        }

        public Task LogAsync(int userId, ActivityAction action, string? details = null, int? projectId = null, int? taskId = null)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action.ToText(),
                Details = details,
                ProjectId = projectId,
                TaskId = taskId
            };

            return _activityLogRepository.CreateAsync(log);
        }
    }
}

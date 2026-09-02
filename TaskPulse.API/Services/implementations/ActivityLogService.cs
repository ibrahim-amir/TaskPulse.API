using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.ActivityLogDto;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Repository.ActivityLogRepo;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Services.Infrastructure;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    public class ActivityLogService : BaseServiceClass, IActivityLogService
    {
        private readonly ActivityLogRepository _activityLogRepository;
        private readonly ProjectRepository _projectRepository;

        public ActivityLogService(
            CustomExceptionHandler customExceptionHandler,
            ActivityLogRepository activityLogRepository,
            ProjectRepository projectRepository)
            : base(customExceptionHandler)
        {
            _activityLogRepository = activityLogRepository;
            _projectRepository = projectRepository;
        }

        public async Task<CommonResponse<PagedResult<ActivityLogDTO>>> GetLogsAsync(ActivityLogQueryDto query, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<PagedResult<ActivityLogDTO>>();

            try
            {
                // The Admin sees the whole trail; everyone else only their own
                // projects, plus their own project-less entries like logins.
                List<int>? visibleProjectIds = callerIsAdmin
                    ? null
                    : await _projectRepository.GetProjectIdsForUserAsync(callerId);

                var page = await _activityLogRepository.GetPagedAsync(query, visibleProjectIds, callerId);
                return CommonResponseFactory.CreateSuccessResponse(page.MapItems(ActivityLogDTO.FromEntity), "Activity retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }
    }
}

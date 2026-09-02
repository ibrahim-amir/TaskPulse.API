using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.DashboardDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Repository.DashboardRepo;
using TaskPulse.API.Repository.NotificationRepo;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Services.Infrastructure;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    public class DashboardService : BaseServiceClass, IDashboardService
    {
        private readonly DashboardRepository _dashboardRepository;
        private readonly ProjectRepository _projectRepository;
        private readonly NotificationRepository _notificationRepository;

        public DashboardService(
            CustomExceptionHandler customExceptionHandler,
            DashboardRepository dashboardRepository,
            ProjectRepository projectRepository,
            NotificationRepository notificationRepository)
            : base(customExceptionHandler)
        {
            _dashboardRepository = dashboardRepository;
            _projectRepository = projectRepository;
            _notificationRepository = notificationRepository;
        }


        // The landing dashboard: headline counters plus the two required charts.
        public async Task<CommonResponse<DashboardSummaryDto>> GetSummaryAsync(int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<DashboardSummaryDto>();

            try
            {
                var scope = await VisibleProjectIdsAsync(callerId, callerIsAdmin);

                var summary = new DashboardSummaryDto
                {
                    ProjectCount = await _dashboardRepository.CountProjectsAsync(scope),
                    TaskCount = await _dashboardRepository.CountTasksAsync(scope),
                    CompletedTaskCount = await _dashboardRepository.CountTasksByStatusAsync(scope, TaskItemStatus.Done),
                    OverdueTaskCount = await _dashboardRepository.CountOverdueTasksAsync(scope),
                    MyOpenTaskCount = await _dashboardRepository.CountOpenTasksForUserAsync(callerId),
                    UnreadNotificationCount = await _notificationRepository.CountUnreadAsync(callerId),
                    PendingApprovalCount = callerIsAdmin ? await _dashboardRepository.CountPendingApprovalsAsync() : 0,
                    TasksByStatus = ToStatusChart(await _dashboardRepository.GroupTasksByStatusAsync(scope)),
                    TasksByPriority = ToPriorityChart(await _dashboardRepository.GroupTasksByPriorityAsync(scope))
                };

                return CommonResponseFactory.CreateSuccessResponse(summary, "Dashboard retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // The per-project report: completion, file progress, the two charts
        // scoped to this project, and the performance table.
        public async Task<CommonResponse<ProjectDashboardDto>> GetProjectDashboardAsync(int projectId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<ProjectDashboardDto>();

            try
            {
                var project = await _projectRepository.GetByIdAsync(projectId);
                if (project is null)
                    return CommonResponseFactory.CreateErrorResponse<ProjectDashboardDto>("Project not found.", HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckMember(project, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<ProjectDashboardDto>(access, HttpStatusCode.Forbidden);

                var taskCount = await _dashboardRepository.CountTasksAsync(null, projectId);
                var completed = await _dashboardRepository.CountTasksByStatusAsync(null, TaskItemStatus.Done, projectId);
                var (required, uploaded) = await _dashboardRepository.CountChecklistProgressAsync(projectId);
                var totals = await _dashboardRepository.GetUserTotalsForProjectAsync(projectId);

                var dashboard = new ProjectDashboardDto
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    TaskCount = taskCount,
                    CompletedTaskCount = completed,
                    OverdueTaskCount = await _dashboardRepository.CountOverdueTasksAsync(null, projectId),
                    CompletionPercent = Percent(completed, taskCount),
                    RequiredFileCount = required,
                    UploadedFileCount = uploaded,
                    FileProgressPercent = Percent(uploaded, required),
                    TasksByStatus = ToStatusChart(await _dashboardRepository.GroupTasksByStatusAsync(null, projectId)),
                    TasksByPriority = ToPriorityChart(await _dashboardRepository.GroupTasksByPriorityAsync(null, projectId)),
                    UserPerformance = totals.Select(t => new UserPerformanceDto
                    {
                        UserId = t.UserId,
                        UserName = t.UserName,
                        AssignedTaskCount = t.AssignedTaskCount,
                        CompletedTaskCount = t.CompletedTaskCount,
                        OverdueTaskCount = t.OverdueTaskCount,
                        CompletionPercent = Percent(t.CompletedTaskCount, t.AssignedTaskCount),
                        EstimatedHours = t.EstimatedHours,
                        ActualHours = t.ActualHours
                    }).ToList()
                };

                return CommonResponseFactory.CreateSuccessResponse(dashboard, "Project report retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Estimated versus actual effort, and how the open work sits against
        // its deadlines.
        public async Task<CommonResponse<ProgressReportDto>> GetProgressAsync(int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<ProgressReportDto>();

            try
            {
                var scope = await VisibleProjectIdsAsync(callerId, callerIsAdmin);

                var (estimated, actual) = await _dashboardRepository.SumHoursAsync(scope);
                var (onTrack, atRisk, late, noDueDate) = await _dashboardRepository.CountDeadlineHealthAsync(scope);
                var projects = await _dashboardRepository.GetProjectsAsync(scope);

                var report = new ProgressReportDto
                {
                    TotalEstimatedHours = estimated,
                    TotalActualHours = actual,
                    EffortVariancePercent = estimated == 0 ? 0 : (int)Math.Round(actual * 100 / estimated),
                    DeadlineHealth = new DeadlineHealthDto
                    {
                        OnTrack = onTrack,
                        AtRisk = atRisk,
                        Late = late,
                        NoDueDate = noDueDate
                    }
                };

                foreach (var project in projects)
                {
                    var taskCount = await _dashboardRepository.CountTasksAsync(null, project.Id);
                    var completed = await _dashboardRepository.CountTasksByStatusAsync(null, TaskItemStatus.Done, project.Id);
                    var (projectEstimated, projectActual) = await _dashboardRepository.SumHoursAsync(null, project.Id);

                    report.Projects.Add(new ProjectProgressDto
                    {
                        ProjectId = project.Id,
                        ProjectName = project.Name,
                        TaskCount = taskCount,
                        CompletedTaskCount = completed,
                        CompletionPercent = Percent(completed, taskCount),
                        OverdueTaskCount = await _dashboardRepository.CountOverdueTasksAsync(null, project.Id),
                        EstimatedHours = projectEstimated,
                        ActualHours = projectActual
                    });
                }

                return CommonResponseFactory.CreateSuccessResponse(report, "Progress report retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // null means "no scoping" — the Admin sees every project.
        private async Task<List<int>?> VisibleProjectIdsAsync(int callerId, bool callerIsAdmin)
        {
            return callerIsAdmin ? null : await _projectRepository.GetProjectIdsForUserAsync(callerId);
        }

        // Every status and priority appears even at zero, so the chart keeps
        // the same columns instead of reshaping itself as data arrives.
        private static List<ChartSliceDto> ToStatusChart(Dictionary<TaskItemStatus, int> counts)
        {
            return Enum.GetValues<TaskItemStatus>()
                .Select(status => new ChartSliceDto
                {
                    Label = status.ToString(),
                    Count = counts.TryGetValue(status, out var count) ? count : 0
                })
                .ToList();
        }

        private static List<ChartSliceDto> ToPriorityChart(Dictionary<PriorityLevel, int> counts)
        {
            return Enum.GetValues<PriorityLevel>()
                .Select(priority => new ChartSliceDto
                {
                    Label = priority.ToString(),
                    Count = counts.TryGetValue(priority, out var count) ? count : 0
                })
                .ToList();
        }

        private static int Percent(int part, int total)
        {
            return total == 0 ? 0 : (int)Math.Round(part * 100.0 / total);
        }
    }
}

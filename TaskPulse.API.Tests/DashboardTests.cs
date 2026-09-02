using System.Net;
using System.Net.Http.Json;
using TaskPulse.API.DTO.DashboardDto;
using TaskPulse.API.DTO.SubtaskDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Tests.Infrastructure;

namespace TaskPulse.API.Tests
{
    public class DashboardTests : ApiTestBase
    {
        public DashboardTests(TaskPulseApiFactory factory) : base(factory) { }

        [Fact]
        public async Task The_Summary_Returns_Both_Required_Charts_With_Every_Column()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            await CreateTaskAsync(scenario);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var body = await ReadAsync<DashboardSummaryDto>(await managerClient.GetAsync("/api/dashboard/summary"));

            Assert.True(body.Success, string.Join("; ", body.Errors));

            // Every status and priority is present even at zero, so the chart
            // keeps its shape instead of columns appearing as data arrives.
            Assert.Equal(Enum.GetValues<TaskItemStatus>().Length, body.Data!.TasksByStatus.Count);
            Assert.Equal(Enum.GetValues<PriorityLevel>().Length, body.Data.TasksByPriority.Count);
            Assert.Contains(body.Data.TasksByStatus, slice => slice.Label == nameof(TaskItemStatus.ToDo));
            Assert.Contains(body.Data.TasksByPriority, slice => slice.Label == nameof(PriorityLevel.Critical));
        }

        [Fact]
        public async Task The_Summary_Counts_The_Callers_Own_Open_Work()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            await CreateTaskAsync(scenario);
            await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            var body = await ReadAsync<DashboardSummaryDto>(await memberClient.GetAsync("/api/dashboard/summary"));

            Assert.True(body.Success);
            Assert.True(body.Data!.MyOpenTaskCount >= 2);
        }

        [Fact]
        public async Task A_Members_Summary_Only_Counts_Projects_They_Are_On()
        {
            await CreateManagerOwnedProjectAsync();
            var outsider = await SeedUserAsync(UserRole.Member);

            using var outsiderClient = await ClientAsAsync(outsider.Email, DefaultPassword);

            var body = await ReadAsync<DashboardSummaryDto>(await outsiderClient.GetAsync("/api/dashboard/summary"));

            Assert.True(body.Success);
            Assert.Equal(0, body.Data!.ProjectCount);
            Assert.Equal(0, body.Data.TaskCount);
        }

        [Fact]
        public async Task Only_The_Admin_Sees_A_Pending_Approval_Count()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            await managerClient.DeleteAsync($"/api/projects/{scenario.ProjectId}");

            var managerView = await ReadAsync<DashboardSummaryDto>(await managerClient.GetAsync("/api/dashboard/summary"));
            Assert.Equal(0, managerView.Data!.PendingApprovalCount);

            using var admin = await AdminClientAsync();
            var adminView = await ReadAsync<DashboardSummaryDto>(await admin.GetAsync("/api/dashboard/summary"));
            Assert.True(adminView.Data!.PendingApprovalCount >= 1);
        }

        [Fact]
        public async Task The_Project_Report_Works_Out_Completion()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var finished = await CreateTaskAsync(scenario, subtaskCount: 1);
            await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);
            using (var content = FileContent("done.pdf"))
                await memberClient.PostAsync($"/api/tasks/{finished.Id}/subtasks/{finished.Subtasks[0].Id}/upload", content);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);
            await managerClient.PostAsync($"/api/tasks/{finished.Id}/close", null);

            var body = await ReadAsync<ProjectDashboardDto>(
                await managerClient.GetAsync($"/api/dashboard/project/{scenario.ProjectId}"));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.Equal(2, body.Data!.TaskCount);
            Assert.Equal(1, body.Data.CompletedTaskCount);
            Assert.Equal(50, body.Data.CompletionPercent);
        }

        [Fact]
        public async Task The_Project_Report_Tracks_Required_File_Progress()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, subtaskCount: 4);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);
            using (var content = FileContent("one.pdf"))
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{task.Subtasks[0].Id}/upload", content);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var body = await ReadAsync<ProjectDashboardDto>(
                await managerClient.GetAsync($"/api/dashboard/project/{scenario.ProjectId}"));

            Assert.Equal(4, body.Data!.RequiredFileCount);
            Assert.Equal(1, body.Data.UploadedFileCount);
            Assert.Equal(25, body.Data.FileProgressPercent);
        }

        [Fact]
        public async Task The_Project_Report_Lists_Everyone_On_The_Project()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            await CreateTaskAsync(scenario);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var body = await ReadAsync<ProjectDashboardDto>(
                await managerClient.GetAsync($"/api/dashboard/project/{scenario.ProjectId}"));

            var memberRow = body.Data!.UserPerformance.SingleOrDefault(u => u.UserId == scenario.Member.Id);

            Assert.NotNull(memberRow);
            Assert.Equal(1, memberRow!.AssignedTaskCount);
            Assert.Contains(body.Data.UserPerformance, u => u.UserId == scenario.Manager.Id);
        }

        [Fact]
        public async Task Someone_Off_The_Project_Cannot_Read_Its_Report()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var outsider = await SeedUserAsync(UserRole.Member);

            using var outsiderClient = await ClientAsAsync(outsider.Email, DefaultPassword);

            var body = await ReadAsync<ProjectDashboardDto>(
                await outsiderClient.GetAsync($"/api/dashboard/project/{scenario.ProjectId}"));

            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.Forbidden, body.StatusCode);
        }

        [Fact]
        public async Task The_Progress_Report_Buckets_Work_By_Deadline()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            await CreateTaskAsync(scenario, dueDate: DateTime.UtcNow.AddDays(-3));
            await CreateTaskAsync(scenario, dueDate: DateTime.UtcNow.AddDays(1));
            await CreateTaskAsync(scenario, dueDate: DateTime.UtcNow.AddDays(30));
            await CreateTaskAsync(scenario);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var body = await ReadAsync<ProgressReportDto>(await managerClient.GetAsync("/api/dashboard/progress"));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.True(body.Data!.DeadlineHealth.Late >= 1);
            Assert.True(body.Data.DeadlineHealth.AtRisk >= 1);
            Assert.True(body.Data.DeadlineHealth.OnTrack >= 1);
            Assert.True(body.Data.DeadlineHealth.NoDueDate >= 1);
        }

        [Fact]
        public async Task The_Progress_Report_Compares_Estimated_And_Actual_Hours()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            // Created with 5 estimated hours; log 10 actual against it.
            await managerClient.PutAsJsonAsync($"/api/tasks/{task.Id}", new
            {
                title = task.Title,
                status = nameof(TaskItemStatus.InProgress),
                priority = nameof(PriorityLevel.Medium),
                estimatedHours = 5m,
                actualHours = 10m
            });

            var body = await ReadAsync<ProgressReportDto>(await managerClient.GetAsync("/api/dashboard/progress"));

            Assert.True(body.Success);
            Assert.True(body.Data!.TotalActualHours >= 10m);

            // Over 100% means it's taking longer than planned.
            Assert.True(body.Data.EffortVariancePercent > 100);
        }

        [Fact]
        public async Task Attendance_Is_On_The_Dashboard_For_The_Caller()
        {
            var member = await SeedUserAsync(UserRole.Member);
            using var memberClient = await ClientAsAsync(member.Email, DefaultPassword);

            var body = await ReadAsync<TaskPulse.API.DTO.AttendanceDto.AttendanceSummaryDto>(
                await memberClient.GetAsync("/api/dashboard/attendance"));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.Equal(member.Id, body.Data!.UserId);
            Assert.True(body.Data.DaysPresent >= 1);
        }
    }
}

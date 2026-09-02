using System.Net.Http.Json;
using TaskPulse.API.DTO.ActivityLogDto;
using TaskPulse.API.DTO.NotificationDto;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Tests.Infrastructure;

namespace TaskPulse.API.Tests
{
    public class NotificationsAndActivityTests : ApiTestBase
    {
        public NotificationsAndActivityTests(TaskPulseApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Being_Assigned_A_Task_Sends_A_Notification()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            var body = await ReadAsync<PagedResult<NotificationDTO>>(
                await memberClient.GetAsync("/api/notifications?pageSize=100"));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.Contains(body.Data!.Items, n => n.Type == NotificationType.TaskAssigned);
        }

        [Fact]
        public async Task Being_Added_To_A_Project_Sends_A_Notification()
        {
            var scenario = await CreateManagerOwnedProjectAsync();

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            var body = await ReadAsync<PagedResult<NotificationDTO>>(
                await memberClient.GetAsync("/api/notifications?pageSize=100"));

            var added = body.Data!.Items.FirstOrDefault(n => n.Type == NotificationType.AddedToProject);

            Assert.NotNull(added);
            Assert.Equal("Project", added!.RelatedEntityType);
            Assert.Equal(scenario.ProjectId, added.RelatedEntityId);
        }

        [Fact]
        public async Task A_Pending_Approval_Notifies_The_Admin()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            await managerClient.DeleteAsync($"/api/projects/{scenario.ProjectId}");

            using var admin = await AdminClientAsync();
            var body = await ReadAsync<PagedResult<NotificationDTO>>(await admin.GetAsync("/api/notifications?pageSize=100"));

            var approval = body.Data!.Items.FirstOrDefault(n => n.Type == NotificationType.ApprovalRequired);

            Assert.NotNull(approval);
            Assert.Equal("ApprovalRequest", approval!.RelatedEntityType);
        }

        [Fact]
        public async Task You_Only_See_Your_Own_Notifications()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            await CreateTaskAsync(scenario);

            var stranger = await SeedUserAsync(UserRole.Member);
            using var strangerClient = await ClientAsAsync(stranger.Email, DefaultPassword);

            var body = await ReadAsync<PagedResult<NotificationDTO>>(
                await strangerClient.GetAsync("/api/notifications?pageSize=100"));

            Assert.True(body.Success);
            Assert.Empty(body.Data!.Items);
        }

        [Fact]
        public async Task A_Notification_Can_Be_Marked_Read()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            var list = await ReadAsync<PagedResult<NotificationDTO>>(await memberClient.GetAsync("/api/notifications?pageSize=100"));
            var first = list.Data!.Items[0];

            var marked = await ReadAsync<NotificationDTO>(await memberClient.PutAsync($"/api/notifications/{first.Id}/read", null));

            Assert.True(marked.Success, string.Join("; ", marked.Errors));
            Assert.True(marked.Data!.IsRead);
        }

        [Fact]
        public async Task Mark_All_Read_Clears_The_Unread_Count()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            var before = await ReadAsync<int>(await memberClient.GetAsync("/api/notifications/unread-count"));
            Assert.True(before.Data > 0);

            var cleared = await ReadAsync<int>(await memberClient.PutAsync("/api/notifications/read-all", null));
            Assert.True(cleared.Success, string.Join("; ", cleared.Errors));

            var after = await ReadAsync<int>(await memberClient.GetAsync("/api/notifications/unread-count"));
            Assert.Equal(0, after.Data);
        }

        [Fact]
        public async Task Someone_Elses_Notification_Cannot_Be_Marked_Read()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);
            var list = await ReadAsync<PagedResult<NotificationDTO>>(await memberClient.GetAsync("/api/notifications?pageSize=100"));
            var theirs = list.Data!.Items[0];

            var stranger = await SeedUserAsync(UserRole.Member);
            using var strangerClient = await ClientAsAsync(stranger.Email, DefaultPassword);

            var attempt = await ReadAsync<NotificationDTO>(await strangerClient.PutAsync($"/api/notifications/{theirs.Id}/read", null));

            // Not-found rather than forbidden, so this can't be used to discover
            // that someone else's notification exists.
            Assert.False(attempt.Success);
            Assert.Equal(System.Net.HttpStatusCode.NotFound, attempt.StatusCode);
        }

        [Fact]
        public async Task Logging_In_Is_Recorded_In_The_Activity_Log()
        {
            var member = await SeedUserAsync(UserRole.Member);
            using var memberClient = await ClientAsAsync(member.Email, DefaultPassword);

            var body = await ReadAsync<PagedResult<ActivityLogDTO>>(
                await memberClient.GetAsync("/api/activity-logs?pageSize=100"));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.Contains(body.Data!.Items, log => log.Action == nameof(ActivityAction.Login) && log.UserId == member.Id);
        }

        [Fact]
        public async Task The_Admin_Sees_Activity_Across_Every_Project()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            await CreateTaskAsync(scenario);

            using var admin = await AdminClientAsync();

            var body = await ReadAsync<PagedResult<ActivityLogDTO>>(
                await admin.GetAsync($"/api/activity-logs?projectId={scenario.ProjectId}&pageSize=100"));

            Assert.True(body.Success);
            Assert.Contains(body.Data!.Items, log => log.Action == nameof(ActivityAction.TaskCreated));
        }

        [Fact]
        public async Task Activity_From_Projects_You_Are_Not_On_Stays_Hidden()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            await CreateTaskAsync(scenario);

            var outsider = await SeedUserAsync(UserRole.Member);
            using var outsiderClient = await ClientAsAsync(outsider.Email, DefaultPassword);

            var body = await ReadAsync<PagedResult<ActivityLogDTO>>(
                await outsiderClient.GetAsync("/api/activity-logs?pageSize=100"));

            Assert.True(body.Success);
            Assert.DoesNotContain(body.Data!.Items, log => log.ProjectId == scenario.ProjectId);

            // They still see their own login, which belongs to no project.
            Assert.Contains(body.Data.Items, log => log.UserId == outsider.Id);
        }
    }
}

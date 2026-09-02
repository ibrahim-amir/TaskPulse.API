using System.Net;
using System.Net.Http.Json;
using TaskPulse.API.DTO.ApprovalDto;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.ProjectDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Tests.Infrastructure;

namespace TaskPulse.API.Tests
{
    // The rule that anything the Admin set up can't be undone without their
    // say-so. These are the tests that prove the gate actually holds.
    public class ApprovalWorkflowTests : ApiTestBase
    {
        public ApprovalWorkflowTests(TaskPulseApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Deleting_An_Admin_Owned_Project_Does_Not_Delete_It()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var attempt = await ReadAsync<string>(await managerClient.DeleteAsync($"/api/projects/{scenario.ProjectId}"));

            // Accepted and queued, not carried out.
            Assert.True(attempt.Success);
            Assert.Equal(HttpStatusCode.Accepted, attempt.StatusCode);

            using var admin = await AdminClientAsync();
            var stillThere = await ReadAsync<ProjectDTO>(await admin.GetAsync($"/api/projects/{scenario.ProjectId}"));
            Assert.True(stillThere.Success);
        }

        [Fact]
        public async Task The_Attempt_Leaves_A_Pending_Request_For_The_Admin()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            await managerClient.DeleteAsync($"/api/projects/{scenario.ProjectId}");

            using var admin = await AdminClientAsync();
            var queue = await ReadAsync<PagedResult<ApprovalRequestDTO>>(await admin.GetAsync("/api/approvals?pageSize=100"));

            var request = queue.Data!.Items.SingleOrDefault(r =>
                r.ActionType == ApprovalActionType.DeleteProject && r.TargetEntityId == scenario.ProjectId);

            Assert.NotNull(request);
            Assert.Equal(ApprovalStatus.Pending, request!.Status);
            Assert.Equal(scenario.Manager.Id, request.RequestedByUserId);
        }

        [Fact]
        public async Task Approving_The_Request_Actually_Deletes_The_Project()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);
            using var admin = await AdminClientAsync();

            await managerClient.DeleteAsync($"/api/projects/{scenario.ProjectId}");
            var requestId = await PendingRequestIdAsync(admin, ApprovalActionType.DeleteProject, scenario.ProjectId);

            var approved = await ReadAsync<ApprovalRequestDTO>(
                await admin.PutAsJsonAsync($"/api/approvals/{requestId}/approve", new { note = "Fine by me." }));

            Assert.True(approved.Success, string.Join("; ", approved.Errors));
            Assert.Equal(ApprovalStatus.Approved, approved.Data!.Status);

            var gone = await ReadAsync<ProjectDTO>(await admin.GetAsync($"/api/projects/{scenario.ProjectId}"));
            Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        }

        [Fact]
        public async Task Rejecting_The_Request_Leaves_The_Project_Alone()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);
            using var admin = await AdminClientAsync();

            await managerClient.DeleteAsync($"/api/projects/{scenario.ProjectId}");
            var requestId = await PendingRequestIdAsync(admin, ApprovalActionType.DeleteProject, scenario.ProjectId);

            var rejected = await ReadAsync<ApprovalRequestDTO>(
                await admin.PutAsJsonAsync($"/api/approvals/{requestId}/reject", new { note = "Still needed." }));

            Assert.True(rejected.Success, string.Join("; ", rejected.Errors));
            Assert.Equal(ApprovalStatus.Rejected, rejected.Data!.Status);
            Assert.Equal("Still needed.", rejected.Data.ResolutionNote);

            var stillThere = await ReadAsync<ProjectDTO>(await admin.GetAsync($"/api/projects/{scenario.ProjectId}"));
            Assert.True(stillThere.Success);
        }

        [Fact]
        public async Task Asking_Twice_Does_Not_Queue_A_Second_Request()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var first = await ReadAsync<string>(await managerClient.DeleteAsync($"/api/projects/{scenario.ProjectId}"));
            var second = await ReadAsync<string>(await managerClient.DeleteAsync($"/api/projects/{scenario.ProjectId}"));

            Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
            Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);
            Assert.Contains(second.Messages, m => m.Contains("already waiting", StringComparison.OrdinalIgnoreCase));

            using var admin = await AdminClientAsync();
            var queue = await ReadAsync<PagedResult<ApprovalRequestDTO>>(await admin.GetAsync("/api/approvals?pageSize=100"));

            var matching = queue.Data!.Items.Count(r =>
                r.ActionType == ApprovalActionType.DeleteProject && r.TargetEntityId == scenario.ProjectId);

            Assert.Equal(1, matching);
        }

        [Fact]
        public async Task Editing_An_Admin_Owned_Project_Also_Needs_Approval_And_Applies_On_Yes()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);
            using var admin = await AdminClientAsync();

            var newName = UniqueName("Renamed By Manager");

            var attempt = await ReadAsync<ProjectDTO>(await managerClient.PutAsJsonAsync($"/api/projects/{scenario.ProjectId}", new
            {
                name = newName,
                status = nameof(ProjectStatus.Completed),
                priority = nameof(PriorityLevel.Critical),
                startDate = DateTime.UtcNow.Date
            }));

            Assert.Equal(HttpStatusCode.Accepted, attempt.StatusCode);

            // Nothing changed yet.
            var unchanged = await ReadAsync<ProjectDTO>(await admin.GetAsync($"/api/projects/{scenario.ProjectId}"));
            Assert.Equal(scenario.ProjectName, unchanged.Data!.Name);

            var requestId = await PendingRequestIdAsync(admin, ApprovalActionType.UpdateProject, scenario.ProjectId);
            await admin.PutAsJsonAsync($"/api/approvals/{requestId}/approve", new { note = (string?)null });

            // The edit the manager asked for is replayed on approval.
            var applied = await ReadAsync<ProjectDTO>(await admin.GetAsync($"/api/projects/{scenario.ProjectId}"));
            Assert.Equal(newName, applied.Data!.Name);
            Assert.Equal(ProjectStatus.Completed, applied.Data.Status);
        }

        [Fact]
        public async Task Removing_A_Member_The_Admin_Added_Needs_Approval()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var attempt = await ReadAsync<string>(
                await managerClient.DeleteAsync($"/api/projects/{scenario.ProjectId}/members/{scenario.Member.Id}"));

            Assert.Equal(HttpStatusCode.Accepted, attempt.StatusCode);

            using var admin = await AdminClientAsync();
            var queue = await ReadAsync<PagedResult<ApprovalRequestDTO>>(await admin.GetAsync("/api/approvals?pageSize=100"));

            Assert.Contains(queue.Data!.Items, r => r.ActionType == ApprovalActionType.RemoveProjectMember);
        }

        [Fact]
        public async Task The_Admin_Never_Needs_Their_Own_Approval()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            using var admin = await AdminClientAsync();

            var deleted = await ReadAsync<string>(await admin.DeleteAsync($"/api/projects/{scenario.ProjectId}"));

            Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);

            var gone = await ReadAsync<ProjectDTO>(await admin.GetAsync($"/api/projects/{scenario.ProjectId}"));
            Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        }

        [Fact]
        public async Task The_Approval_Queue_Is_Admin_Only()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var response = await managerClient.GetAsync("/api/approvals");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task A_Request_Cannot_Be_Answered_Twice()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);
            using var admin = await AdminClientAsync();

            await managerClient.DeleteAsync($"/api/projects/{scenario.ProjectId}");
            var requestId = await PendingRequestIdAsync(admin, ApprovalActionType.DeleteProject, scenario.ProjectId);

            await admin.PutAsJsonAsync($"/api/approvals/{requestId}/approve", new { note = (string?)null });

            var again = await ReadAsync<ApprovalRequestDTO>(
                await admin.PutAsJsonAsync($"/api/approvals/{requestId}/reject", new { note = "Changed my mind." }));

            Assert.False(again.Success);
            Assert.Contains(again.Errors, e => e.Contains("already", StringComparison.OrdinalIgnoreCase));
        }

        private async Task<int> PendingRequestIdAsync(HttpClient admin, ApprovalActionType actionType, int targetId)
        {
            var queue = await ReadAsync<PagedResult<ApprovalRequestDTO>>(await admin.GetAsync("/api/approvals?pageSize=100"));

            var request = queue.Data!.Items.SingleOrDefault(r =>
                r.ActionType == actionType && r.TargetEntityId == targetId && r.Status == ApprovalStatus.Pending);

            Assert.NotNull(request);
            return request!.Id;
        }
    }
}

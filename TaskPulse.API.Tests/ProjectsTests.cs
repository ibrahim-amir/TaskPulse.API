using System.Net;
using System.Net.Http.Json;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.ProjectDto;
using TaskPulse.API.DTO.ProjectMemberDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Tests.Infrastructure;

namespace TaskPulse.API.Tests
{
    public class ProjectsTests : ApiTestBase
    {
        public ProjectsTests(TaskPulseApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Admin_Creating_A_Project_Must_Name_A_Manager()
        {
            using var admin = await AdminClientAsync();

            var body = await ReadAsync<ProjectDTO>(await admin.PostAsJsonAsync("/api/projects", new
            {
                name = UniqueName("Unmanaged"),
                status = nameof(ProjectStatus.Active),
                priority = nameof(PriorityLevel.Medium),
                startDate = DateTime.UtcNow.Date
            }));

            // The Admin doesn't run projects, so one has to be handed to someone.
            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.BadRequest, body.StatusCode);
        }

        [Fact]
        public async Task Admin_Created_Project_Is_Stamped_As_Admin_Owned()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            using var admin = await AdminClientAsync();

            var body = await ReadAsync<ProjectDTO>(await admin.GetAsync($"/api/projects/{scenario.ProjectId}"));

            Assert.True(body.Success);
            Assert.Equal(UserRole.Admin, body.Data!.CreatedByRole);
        }

        [Fact]
        public async Task A_Manager_Creating_Their_Own_Project_Runs_It()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var project = await ReadAsync<ProjectDTO>(await managerClient.GetAsync($"/api/projects/{scenario.ProjectId}"));
            Assert.Equal(UserRole.Manager, project.Data!.CreatedByRole);

            var members = await ReadAsync<List<ProjectMemberDTO>>(
                await managerClient.GetAsync($"/api/projects/{scenario.ProjectId}/members"));

            Assert.Contains(members.Data!, m => m.UserId == scenario.Manager.Id && m.Role == UserRole.Manager);
        }

        [Fact]
        public async Task A_Member_Cannot_Create_A_Project()
        {
            var member = await SeedUserAsync(UserRole.Member);
            using var memberClient = await ClientAsAsync(member.Email, DefaultPassword);

            var response = await memberClient.PostAsJsonAsync("/api/projects", new
            {
                name = UniqueName("Not Allowed"),
                status = nameof(ProjectStatus.Active),
                priority = nameof(PriorityLevel.Medium),
                startDate = DateTime.UtcNow.Date
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Project_Names_Have_To_Be_Unique()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            var manager = await SeedUserAsync(UserRole.Manager);

            using var admin = await AdminClientAsync();

            var body = await ReadAsync<ProjectDTO>(await admin.PostAsJsonAsync("/api/projects", new
            {
                name = scenario.ProjectName,
                status = nameof(ProjectStatus.Active),
                priority = nameof(PriorityLevel.Medium),
                startDate = DateTime.UtcNow.Date,
                managerUserId = manager.Id
            }));

            Assert.False(body.Success);
        }

        [Fact]
        public async Task End_Date_Cannot_Be_Before_Start_Date()
        {
            var manager = await SeedUserAsync(UserRole.Manager);
            using var admin = await AdminClientAsync();

            var body = await ReadAsync<ProjectDTO>(await admin.PostAsJsonAsync("/api/projects", new
            {
                name = UniqueName("Backwards"),
                status = nameof(ProjectStatus.Active),
                priority = nameof(PriorityLevel.Medium),
                startDate = DateTime.UtcNow.Date,
                endDate = DateTime.UtcNow.Date.AddDays(-5),
                managerUserId = manager.Id
            }));

            Assert.False(body.Success);
        }

        [Fact]
        public async Task People_Only_See_Projects_They_Are_On()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            var outsider = await SeedUserAsync(UserRole.Member);

            using var outsiderClient = await ClientAsAsync(outsider.Email, DefaultPassword);

            var body = await ReadAsync<PagedResult<ProjectDTO>>(
                await outsiderClient.GetAsync("/api/projects?pageSize=100"));

            Assert.True(body.Success);
            Assert.DoesNotContain(body.Data!.Items, p => p.Id == scenario.ProjectId);
        }

        [Fact]
        public async Task A_Non_Member_Cannot_Open_A_Project()
        {
            var scenario = await CreateAdminOwnedProjectAsync();
            var outsider = await SeedUserAsync(UserRole.Member);

            using var outsiderClient = await ClientAsAsync(outsider.Email, DefaultPassword);

            var body = await ReadAsync<ProjectDTO>(await outsiderClient.GetAsync($"/api/projects/{scenario.ProjectId}"));

            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.Forbidden, body.StatusCode);
        }

        [Fact]
        public async Task The_Admin_Sees_Every_Project()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            using var admin = await AdminClientAsync();

            var body = await ReadAsync<PagedResult<ProjectDTO>>(await admin.GetAsync("/api/projects?pageSize=100"));

            Assert.Contains(body.Data!.Items, p => p.Id == scenario.ProjectId);
        }

        [Fact]
        public async Task A_Member_Cannot_Change_A_Project_They_Are_On()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            // Being on a project lets you read it, not run it.
            var response = await memberClient.PutAsJsonAsync($"/api/projects/{scenario.ProjectId}", new
            {
                name = UniqueName("Member Edit"),
                status = nameof(ProjectStatus.Active),
                priority = nameof(PriorityLevel.High),
                startDate = DateTime.UtcNow.Date
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task A_Manager_Can_Edit_Their_Own_Project_Immediately()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var newName = UniqueName("Renamed Project");

            var body = await ReadAsync<ProjectDTO>(await managerClient.PutAsJsonAsync($"/api/projects/{scenario.ProjectId}", new
            {
                name = newName,
                status = nameof(ProjectStatus.Active),
                priority = nameof(PriorityLevel.High),
                startDate = DateTime.UtcNow.Date
            }));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.Equal(HttpStatusCode.OK, body.StatusCode);
            Assert.Equal(newName, body.Data!.Name);
        }

        [Fact]
        public async Task A_Manager_Can_Delete_Their_Own_Project_Immediately()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var deleted = await ReadAsync<string>(await managerClient.DeleteAsync($"/api/projects/{scenario.ProjectId}"));

            Assert.True(deleted.Success, string.Join("; ", deleted.Errors));
            Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);

            using var admin = await AdminClientAsync();
            var gone = await ReadAsync<ProjectDTO>(await admin.GetAsync($"/api/projects/{scenario.ProjectId}"));
            Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        }

        [Fact]
        public async Task A_Member_Added_By_A_Manager_Can_Be_Removed_By_That_Manager()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var removed = await ReadAsync<string>(
                await managerClient.DeleteAsync($"/api/projects/{scenario.ProjectId}/members/{scenario.Member.Id}"));

            Assert.True(removed.Success, string.Join("; ", removed.Errors));

            var members = await ReadAsync<List<ProjectMemberDTO>>(
                await managerClient.GetAsync($"/api/projects/{scenario.ProjectId}/members"));

            Assert.DoesNotContain(members.Data!, m => m.UserId == scenario.Member.Id);
        }

        [Fact]
        public async Task The_Same_Person_Cannot_Be_Added_Twice()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var body = await ReadAsync<ProjectMemberDTO>(
                await managerClient.PostAsJsonAsync($"/api/projects/{scenario.ProjectId}/members", new
                {
                    userId = scenario.Member.Id,
                    role = nameof(UserRole.Member)
                }));

            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.BadRequest, body.StatusCode);
        }

        [Fact]
        public async Task A_Disabled_Account_Cannot_Be_Added_To_A_Project()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var disabled = await SeedUserAsync(UserRole.Member);

            using var admin = await AdminClientAsync();
            await admin.DeleteAsync($"/api/users/{disabled.Id}");

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var body = await ReadAsync<ProjectMemberDTO>(
                await managerClient.PostAsJsonAsync($"/api/projects/{scenario.ProjectId}/members", new
                {
                    userId = disabled.Id,
                    role = nameof(UserRole.Member)
                }));

            Assert.False(body.Success);
        }
    }
}

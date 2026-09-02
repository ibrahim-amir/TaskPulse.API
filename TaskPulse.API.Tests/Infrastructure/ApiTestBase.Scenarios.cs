using System.Net.Http.Json;
using TaskPulse.API.DTO.ProjectDto;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Tests.Infrastructure
{
    // Most tests need the same starting point: a project, someone managing it,
    // someone working on it. Building that through the real endpoints (rather
    // than seeding rows) means the setup itself is exercising the API.
    public abstract partial class ApiTestBase
    {
        // A project created by the Admin, so it's Admin-owned and its changes
        // fall under the approval gate.
        protected async Task<TeamScenario> CreateAdminOwnedProjectAsync()
        {
            var manager = await SeedUserAsync(UserRole.Manager);
            var member = await SeedUserAsync(UserRole.Member);

            using var admin = await AdminClientAsync();

            var created = await admin.PostAsJsonAsync("/api/projects", new
            {
                name = UniqueName("Admin Project"),
                description = "Created by the admin for tests.",
                status = nameof(ProjectStatus.Active),
                priority = nameof(PriorityLevel.Medium),
                startDate = DateTime.UtcNow.Date,
                endDate = DateTime.UtcNow.Date.AddDays(30),
                managerUserId = manager.Id
            });

            var project = await ReadAsync<ProjectDTO>(created);
            Assert.True(project.Success, string.Join("; ", project.Errors));

            var projectId = project.Data!.Id;

            var added = await admin.PostAsJsonAsync($"/api/projects/{projectId}/members", new
            {
                userId = member.Id,
                role = nameof(UserRole.Member)
            });
            Assert.True((await ReadAsync<object>(added)).Success);

            return new TeamScenario(projectId, project.Data!.Name, manager, member);
        }

        // The same shape, but the Manager created it themselves — so nothing
        // about it needs the Admin's approval.
        protected async Task<TeamScenario> CreateManagerOwnedProjectAsync()
        {
            var manager = await SeedUserAsync(UserRole.Manager);
            var member = await SeedUserAsync(UserRole.Member);

            using var managerClient = await ClientAsAsync(manager.Email, DefaultPassword);

            var created = await managerClient.PostAsJsonAsync("/api/projects", new
            {
                name = UniqueName("Manager Project"),
                description = "Created by a manager for tests.",
                status = nameof(ProjectStatus.Active),
                priority = nameof(PriorityLevel.Medium),
                startDate = DateTime.UtcNow.Date,
                endDate = DateTime.UtcNow.Date.AddDays(30)
            });

            var project = await ReadAsync<ProjectDTO>(created);
            Assert.True(project.Success, string.Join("; ", project.Errors));

            var projectId = project.Data!.Id;

            var added = await managerClient.PostAsJsonAsync($"/api/projects/{projectId}/members", new
            {
                userId = member.Id,
                role = nameof(UserRole.Member)
            });
            Assert.True((await ReadAsync<object>(added)).Success);

            return new TeamScenario(projectId, project.Data!.Name, manager, member);
        }

        // A task on the scenario's project, assigned to its member, with as many
        // required-file slots as asked for.
        protected async Task<TaskDTO> CreateTaskAsync(
            TeamScenario scenario,
            int subtaskCount = 0,
            DateTime? dueDate = null,
            bool assignToMember = true)
        {
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var labels = Enumerable.Range(1, subtaskCount).Select(i => $"Required file {i}").ToList();

            var created = await managerClient.PostAsJsonAsync("/api/tasks", new
            {
                projectId = scenario.ProjectId,
                title = UniqueName("Task"),
                description = "Created for tests.",
                priority = nameof(PriorityLevel.Medium),
                dueDate,
                estimatedHours = 5m,
                assignedUserIds = assignToMember ? new[] { scenario.Member.Id } : Array.Empty<int>(),
                subtaskLabels = labels
            });

            var task = await ReadAsync<TaskDTO>(created);
            Assert.True(task.Success, string.Join("; ", task.Errors));

            return task.Data!;
        }

        // Multipart upload, matching the IFormFile parameter named "file".
        protected static MultipartFormDataContent FileContent(string fileName, string contents = "test file contents", string contentType = "application/pdf")
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(contents);
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            return new MultipartFormDataContent { { fileContent, "file", fileName } };
        }
    }

    // A project plus the two people on it, which is the starting point for most
    // of the permission tests.
    public record TeamScenario(int ProjectId, string ProjectName, User Manager, User Member);
}

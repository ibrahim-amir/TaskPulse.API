using System.Net;
using System.Net.Http.Json;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Tests.Infrastructure;

namespace TaskPulse.API.Tests
{
    public class TasksTests : ApiTestBase
    {
        public TasksTests(TaskPulseApiFactory factory) : base(factory) { }

        [Fact]
        public async Task A_Manager_Can_Create_A_Task_With_A_File_Checklist()
        {
            var scenario = await CreateManagerOwnedProjectAsync();

            var task = await CreateTaskAsync(scenario, subtaskCount: 3);

            Assert.Equal(TaskItemStatus.ToDo, task.Status);
            Assert.Equal(3, task.RequiredFileCount);
            Assert.Equal(0, task.UploadedFileCount);
            Assert.Equal(0, task.ProgressPercent);
            Assert.Contains(scenario.Member.Id, task.AssignedUserIds);
        }

        [Fact]
        public async Task A_Member_Cannot_Create_A_Task()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            var response = await memberClient.PostAsJsonAsync("/api/tasks", new
            {
                projectId = scenario.ProjectId,
                title = UniqueName("Sneaky"),
                priority = nameof(PriorityLevel.Low)
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Someone_Not_On_The_Project_Cannot_Be_Assigned()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var outsider = await SeedUserAsync(UserRole.Member);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var body = await ReadAsync<TaskDTO>(await managerClient.PostAsJsonAsync("/api/tasks", new
            {
                projectId = scenario.ProjectId,
                title = UniqueName("Outsider Task"),
                priority = nameof(PriorityLevel.Low),
                assignedUserIds = new[] { outsider.Id }
            }));

            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.BadRequest, body.StatusCode);
        }

        [Fact]
        public async Task An_Assigned_Member_Can_Move_Their_Card()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            var body = await ReadAsync<TaskDTO>(await memberClient.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new
            {
                status = nameof(TaskItemStatus.InProgress)
            }));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.Equal(TaskItemStatus.InProgress, body.Data!.Status);
        }

        [Fact]
        public async Task An_Unassigned_Member_Cannot_Move_The_Card()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, assignToMember: false);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            var body = await ReadAsync<TaskDTO>(await memberClient.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new
            {
                status = nameof(TaskItemStatus.InProgress)
            }));

            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.Forbidden, body.StatusCode);
        }

        [Fact]
        public async Task A_Member_Cannot_Drag_A_Card_Straight_To_Done()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            var body = await ReadAsync<TaskDTO>(await memberClient.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new
            {
                status = nameof(TaskItemStatus.Done)
            }));

            // Done is reached by a manager closing the task after review.
            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.Forbidden, body.StatusCode);
        }

        [Fact]
        public async Task A_Member_Cannot_Edit_Anything_Else_About_The_Task()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            var response = await memberClient.PutAsJsonAsync($"/api/tasks/{task.Id}", new
            {
                title = "Rewritten by a member",
                status = nameof(TaskItemStatus.InProgress),
                priority = nameof(PriorityLevel.Critical)
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task A_Manager_Can_Edit_The_Whole_Task()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);
            var newTitle = UniqueName("Updated Task");

            var body = await ReadAsync<TaskDTO>(await managerClient.PutAsJsonAsync($"/api/tasks/{task.Id}", new
            {
                title = newTitle,
                description = "Updated.",
                status = nameof(TaskItemStatus.InProgress),
                priority = nameof(PriorityLevel.High),
                dueDate = DateTime.UtcNow.AddDays(7),
                estimatedHours = 8m,
                actualHours = 2m
            }));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.Equal(newTitle, body.Data!.Title);
            Assert.Equal(PriorityLevel.High, body.Data.Priority);
        }

        [Fact]
        public async Task Tasks_Can_Be_Filtered_By_Status()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var moved = await CreateTaskAsync(scenario);
            await CreateTaskAsync(scenario);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);
            await managerClient.PatchAsJsonAsync($"/api/tasks/{moved.Id}/status", new { status = nameof(TaskItemStatus.InProgress) });

            var body = await ReadAsync<PagedResult<TaskDTO>>(
                await managerClient.GetAsync($"/api/projects/{scenario.ProjectId}/tasks?status=InProgress&pageSize=100"));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.All(body.Data!.Items, t => Assert.Equal(TaskItemStatus.InProgress, t.Status));
            Assert.Contains(body.Data.Items, t => t.Id == moved.Id);
        }

        [Fact]
        public async Task Tasks_Can_Be_Searched_By_Title()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var body = await ReadAsync<PagedResult<TaskDTO>>(
                await managerClient.GetAsync($"/api/projects/{scenario.ProjectId}/tasks?search={Uri.EscapeDataString(task.Title)}"));

            Assert.True(body.Success);
            Assert.Single(body.Data!.Items);
            Assert.Equal(task.Id, body.Data.Items[0].Id);
        }

        [Fact]
        public async Task Task_Paging_Reports_The_Right_Numbers()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            await CreateTaskAsync(scenario);
            await CreateTaskAsync(scenario);
            await CreateTaskAsync(scenario);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var body = await ReadAsync<PagedResult<TaskDTO>>(
                await managerClient.GetAsync($"/api/projects/{scenario.ProjectId}/tasks?page=1&pageSize=2"));

            Assert.True(body.Success);
            Assert.Equal(2, body.Data!.Items.Count);
            Assert.Equal(3, body.Data.TotalCount);
            Assert.Equal(2, body.Data.TotalPages);
            Assert.True(body.Data.HasNext);
            Assert.False(body.Data.HasPrevious);
        }

        [Fact]
        public async Task Assignees_Can_Be_Replaced_In_One_Call()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var body = await ReadAsync<TaskDTO>(await managerClient.PutAsJsonAsync($"/api/tasks/{task.Id}/assignees", new
            {
                userIds = new[] { scenario.Manager.Id }
            }));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.Equal(new[] { scenario.Manager.Id }, body.Data!.AssignedUserIds);
        }

        [Fact]
        public async Task A_Task_Past_Its_Due_Date_Reads_As_Overdue()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, dueDate: DateTime.UtcNow.AddDays(-2));

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var body = await ReadAsync<TaskDTO>(await managerClient.GetAsync($"/api/tasks/{task.Id}"));

            Assert.True(body.Success);
            Assert.True(body.Data!.IsOverdue);
        }

        [Fact]
        public async Task Someone_Off_The_Project_Cannot_Open_Its_Tasks()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);
            var outsider = await SeedUserAsync(UserRole.Member);

            using var outsiderClient = await ClientAsAsync(outsider.Email, DefaultPassword);

            var body = await ReadAsync<TaskDTO>(await outsiderClient.GetAsync($"/api/tasks/{task.Id}"));

            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.Forbidden, body.StatusCode);
        }

        [Fact]
        public async Task A_Manager_Can_Delete_A_Task()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var deleted = await ReadAsync<string>(await managerClient.DeleteAsync($"/api/tasks/{task.Id}"));
            Assert.True(deleted.Success, string.Join("; ", deleted.Errors));

            var gone = await ReadAsync<TaskDTO>(await managerClient.GetAsync($"/api/tasks/{task.Id}"));
            Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        }

        [Fact]
        public async Task A_Task_Without_A_Title_Is_Rejected()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var body = await ReadAsync<TaskDTO>(await managerClient.PostAsJsonAsync("/api/tasks", new
            {
                projectId = scenario.ProjectId,
                title = "",
                priority = nameof(PriorityLevel.Low)
            }));

            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.BadRequest, body.StatusCode);
        }
    }
}

using System.Net;
using System.Net.Http.Json;
using TaskPulse.API.DTO.CommentDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Tests.Infrastructure;

namespace TaskPulse.API.Tests
{
    public class CommentsTests : ApiTestBase
    {
        public CommentsTests(TaskPulseApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Any_Member_Of_The_Project_Can_Comment()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            var body = await ReadAsync<CommentDTO>(
                await memberClient.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new { body = "Looks good to me." }));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.Equal("Looks good to me.", body.Data!.Body);
            Assert.Equal(scenario.Member.Id, body.Data.UserId);
        }

        [Fact]
        public async Task Someone_Off_The_Project_Cannot_Comment()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);
            var outsider = await SeedUserAsync(UserRole.Member);

            using var outsiderClient = await ClientAsAsync(outsider.Email, DefaultPassword);

            var body = await ReadAsync<CommentDTO>(
                await outsiderClient.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new { body = "Butting in." }));

            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.Forbidden, body.StatusCode);
        }

        [Fact]
        public async Task An_Empty_Comment_Is_Rejected()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            var body = await ReadAsync<CommentDTO>(
                await memberClient.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new { body = "   " }));

            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.BadRequest, body.StatusCode);
        }

        [Fact]
        public async Task Comments_Come_Back_Oldest_First()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            await memberClient.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new { body = "First." });
            await memberClient.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new { body = "Second." });

            var body = await ReadAsync<List<CommentDTO>>(await memberClient.GetAsync($"/api/tasks/{task.Id}/comments"));

            Assert.True(body.Success);
            Assert.Equal(2, body.Data!.Count);
            Assert.Equal("First.", body.Data[0].Body);
            Assert.Equal("Second.", body.Data[1].Body);
        }

        [Fact]
        public async Task The_Author_Can_Edit_Their_Own_Comment()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            var created = await ReadAsync<CommentDTO>(
                await memberClient.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new { body = "Original." }));

            var edited = await ReadAsync<CommentDTO>(
                await memberClient.PutAsJsonAsync($"/api/comments/{created.Data!.Id}", new { body = "Edited." }));

            Assert.True(edited.Success, string.Join("; ", edited.Errors));
            Assert.Equal("Edited.", edited.Data!.Body);
            Assert.NotNull(edited.Data.UpdatedAt);
        }

        [Fact]
        public async Task Nobody_Else_Can_Edit_Your_Comment_Not_Even_A_Manager()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);
            var created = await ReadAsync<CommentDTO>(
                await memberClient.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new { body = "Mine." }));

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);
            var edited = await ReadAsync<CommentDTO>(
                await managerClient.PutAsJsonAsync($"/api/comments/{created.Data!.Id}", new { body = "Rewritten." }));

            // Moderating is one thing; putting words in someone's mouth is another.
            Assert.False(edited.Success);
            Assert.Equal(HttpStatusCode.Forbidden, edited.StatusCode);
        }

        [Fact]
        public async Task A_Manager_Can_Delete_Someone_Elses_Comment()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);
            var created = await ReadAsync<CommentDTO>(
                await memberClient.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new { body = "Off topic." }));

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);
            var deleted = await ReadAsync<string>(await managerClient.DeleteAsync($"/api/comments/{created.Data!.Id}"));

            Assert.True(deleted.Success, string.Join("; ", deleted.Errors));

            var remaining = await ReadAsync<List<CommentDTO>>(await memberClient.GetAsync($"/api/tasks/{task.Id}/comments"));
            Assert.DoesNotContain(remaining.Data!, c => c.Id == created.Data.Id);
        }

        [Fact]
        public async Task A_Member_Cannot_Delete_Someone_Elses_Comment()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);
            var created = await ReadAsync<CommentDTO>(
                await managerClient.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new { body = "Manager note." }));

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);
            var deleted = await ReadAsync<string>(await memberClient.DeleteAsync($"/api/comments/{created.Data!.Id}"));

            Assert.False(deleted.Success);
            Assert.Equal(HttpStatusCode.Forbidden, deleted.StatusCode);
        }
    }
}

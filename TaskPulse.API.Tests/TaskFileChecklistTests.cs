using System.Net;
using System.Net.Http.Json;
using TaskPulse.API.DTO.AttachmentDto;
using TaskPulse.API.DTO.SubtaskDto;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Tests.Infrastructure;

namespace TaskPulse.API.Tests
{
    // The subtasks-are-required-files design: progress, the automatic move to
    // Review, reupload overwriting, and closing after review.
    public class TaskFileChecklistTests : ApiTestBase
    {
        public TaskFileChecklistTests(TaskPulseApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Uploading_A_File_Fills_Its_Slot_And_Moves_The_Progress_Bar()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, subtaskCount: 2);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            using var content = FileContent("contract.pdf");
            var uploaded = await ReadAsync<SubtaskDTO>(
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{task.Subtasks[0].Id}/upload", content));

            Assert.True(uploaded.Success, string.Join("; ", uploaded.Errors));
            Assert.True(uploaded.Data!.IsUploaded);
            Assert.Equal("contract.pdf", uploaded.Data.FileName);

            var refreshed = await ReadAsync<TaskDTO>(await memberClient.GetAsync($"/api/tasks/{task.Id}"));
            Assert.Equal(1, refreshed.Data!.UploadedFileCount);
            Assert.Equal(50, refreshed.Data.ProgressPercent);
        }

        [Fact]
        public async Task The_Last_Required_File_Moves_The_Task_To_Review()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, subtaskCount: 2);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            foreach (var subtask in task.Subtasks)
            {
                using var content = FileContent($"file-{subtask.Id}.pdf");
                var uploaded = await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{subtask.Id}/upload", content);
                Assert.True((await ReadAsync<SubtaskDTO>(uploaded)).Success);
            }

            var refreshed = await ReadAsync<TaskDTO>(await memberClient.GetAsync($"/api/tasks/{task.Id}"));

            Assert.Equal(100, refreshed.Data!.ProgressPercent);
            Assert.Equal(TaskItemStatus.Review, refreshed.Data.Status);
        }

        [Fact]
        public async Task Reuploading_Replaces_The_File_Instead_Of_Keeping_Both()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, subtaskCount: 1);
            var subtaskId = task.Subtasks[0].Id;

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            using (var first = FileContent("first.pdf"))
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{subtaskId}/upload", first);

            using (var second = FileContent("second.pdf"))
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{subtaskId}/upload", second);

            var attachments = await ReadAsync<List<AttachmentDTO>>(
                await memberClient.GetAsync($"/api/tasks/{task.Id}/attachments"));

            // Overwrite, no version history — that was the explicit design call.
            Assert.Single(attachments.Data!);
            Assert.Equal("second.pdf", attachments.Data![0].FileName);
        }

        [Fact]
        public async Task A_Disallowed_File_Type_Is_Refused()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, subtaskCount: 1);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            using var content = FileContent("payload.exe", contentType: "application/octet-stream");
            var body = await ReadAsync<SubtaskDTO>(
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{task.Subtasks[0].Id}/upload", content));

            Assert.False(body.Success);
            Assert.Contains(body.Errors, e => e.Contains("not allowed", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task A_File_Disguised_With_A_Second_Extension_Is_Refused()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, subtaskCount: 1);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            using var content = FileContent("invoice.pdf.exe", contentType: "application/pdf");
            var body = await ReadAsync<SubtaskDTO>(
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{task.Subtasks[0].Id}/upload", content));

            Assert.False(body.Success);
        }

        [Fact]
        public async Task A_File_Over_The_Size_Limit_Is_Refused()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, subtaskCount: 1);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            // The configured limit is 10 MB.
            var oversized = new string('x', 11 * 1024 * 1024);
            using var content = FileContent("huge.pdf", oversized);

            var body = await ReadAsync<SubtaskDTO>(
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{task.Subtasks[0].Id}/upload", content));

            Assert.False(body.Success);
            Assert.Contains(body.Errors, e => e.Contains("larger than", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Someone_Not_On_The_Task_Cannot_Upload_To_It()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, subtaskCount: 1, assignToMember: false);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            using var content = FileContent("notmine.pdf");
            var body = await ReadAsync<SubtaskDTO>(
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{task.Subtasks[0].Id}/upload", content));

            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.Forbidden, body.StatusCode);
        }

        [Fact]
        public async Task A_Task_Cannot_Be_Closed_With_Files_Still_Missing()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, subtaskCount: 2);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);
            using (var content = FileContent("only-one.pdf"))
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{task.Subtasks[0].Id}/upload", content);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);
            var body = await ReadAsync<TaskDTO>(await managerClient.PostAsync($"/api/tasks/{task.Id}/close", null));

            Assert.False(body.Success);
            Assert.Contains(body.Errors, e => e.Contains("1 of 2", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Closing_A_Finished_Task_Marks_It_Done_And_Stamps_The_Date()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, subtaskCount: 1);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);
            using (var content = FileContent("done.pdf"))
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{task.Subtasks[0].Id}/upload", content);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);
            var body = await ReadAsync<TaskDTO>(await managerClient.PostAsync($"/api/tasks/{task.Id}/close", null));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.Equal(TaskItemStatus.Done, body.Data!.Status);
            Assert.NotNull(body.Data.CompletedDate);
        }

        [Fact]
        public async Task A_Member_Cannot_Close_A_Task()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, subtaskCount: 1);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);
            using (var content = FileContent("done.pdf"))
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{task.Subtasks[0].Id}/upload", content);

            var response = await memberClient.PostAsync($"/api/tasks/{task.Id}/close", null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Nothing_More_Can_Be_Uploaded_Once_The_Task_Is_Closed()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, subtaskCount: 1);
            var subtaskId = task.Subtasks[0].Id;

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);
            using (var first = FileContent("done.pdf"))
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{subtaskId}/upload", first);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);
            await managerClient.PostAsync($"/api/tasks/{task.Id}/close", null);

            using var late = FileContent("too-late.pdf");
            var body = await ReadAsync<SubtaskDTO>(
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{subtaskId}/upload", late));

            Assert.False(body.Success);
            Assert.Contains(body.Errors, e => e.Contains("closed", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Removing_An_Uploaded_File_Reopens_Its_Slot()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, subtaskCount: 1);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);
            using (var content = FileContent("temporary.pdf"))
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{task.Subtasks[0].Id}/upload", content);

            var attachments = await ReadAsync<List<AttachmentDTO>>(
                await memberClient.GetAsync($"/api/tasks/{task.Id}/attachments"));

            var removed = await ReadAsync<string>(
                await memberClient.DeleteAsync($"/api/tasks/{task.Id}/attachments/{attachments.Data![0].Id}"));
            Assert.True(removed.Success, string.Join("; ", removed.Errors));

            var refreshed = await ReadAsync<TaskDTO>(await memberClient.GetAsync($"/api/tasks/{task.Id}"));

            // The progress bar has to tell the truth again.
            Assert.Equal(0, refreshed.Data!.UploadedFileCount);
            Assert.NotEqual(TaskItemStatus.Review, refreshed.Data.Status);
        }

        [Fact]
        public async Task An_Uploaded_File_Can_Be_Downloaded_Again()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario, subtaskCount: 1);
            const string contents = "the actual bytes";

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);
            using (var content = FileContent("report.pdf", contents))
                await memberClient.PostAsync($"/api/tasks/{task.Id}/subtasks/{task.Subtasks[0].Id}/upload", content);

            var attachments = await ReadAsync<List<AttachmentDTO>>(
                await memberClient.GetAsync($"/api/tasks/{task.Id}/attachments"));

            var download = await memberClient.GetAsync($"/api/tasks/{task.Id}/attachments/{attachments.Data![0].Id}/download");

            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal(contents, await download.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task A_Manager_Can_Add_A_Required_File_To_An_Existing_Task()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var managerClient = await ClientAsAsync(scenario.Manager.Email, DefaultPassword);

            var added = await ReadAsync<SubtaskDTO>(await managerClient.PostAsJsonAsync($"/api/tasks/{task.Id}/subtasks", new
            {
                label = "Signed contract"
            }));

            Assert.True(added.Success, string.Join("; ", added.Errors));
            Assert.Equal("Signed contract", added.Data!.Label);
            Assert.False(added.Data.IsUploaded);
        }

        [Fact]
        public async Task A_Member_Cannot_Add_Required_Files()
        {
            var scenario = await CreateManagerOwnedProjectAsync();
            var task = await CreateTaskAsync(scenario);

            using var memberClient = await ClientAsAsync(scenario.Member.Email, DefaultPassword);

            var response = await memberClient.PostAsJsonAsync($"/api/tasks/{task.Id}/subtasks", new { label = "Made up" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}

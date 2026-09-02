using System.Net;
using System.Net.Http.Json;
using TaskPulse.API.DTO.AttendanceDto;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Tests.Infrastructure;

namespace TaskPulse.API.Tests
{
    public class UsersTests : ApiTestBase
    {
        public UsersTests(TaskPulseApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Admin_Can_List_Users_With_Paging()
        {
            await SeedUserAsync(UserRole.Member);
            using var admin = await AdminClientAsync();

            var body = await ReadAsync<PagedResult<UserDTO>>(await admin.GetAsync("/api/users?page=1&pageSize=5"));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.Equal(1, body.Data!.Page);
            Assert.Equal(5, body.Data.PageSize);
            Assert.True(body.Data.TotalCount >= 2);
            Assert.True(body.Data.Items.Count <= 5);
        }

        [Fact]
        public async Task Listing_Users_Is_Admin_Only()
        {
            var member = await SeedUserAsync(UserRole.Member);
            using var memberClient = await ClientAsAsync(member.Email, DefaultPassword);

            var response = await memberClient.GetAsync("/api/users");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Users_Can_Be_Filtered_By_Role()
        {
            await SeedUserAsync(UserRole.Manager);
            using var admin = await AdminClientAsync();

            var body = await ReadAsync<PagedResult<UserDTO>>(await admin.GetAsync("/api/users?role=Manager&pageSize=100"));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.NotEmpty(body.Data!.Items);
            Assert.All(body.Data.Items, user => Assert.Equal(UserRole.Manager, user.Role));
        }

        [Fact]
        public async Task A_Member_Can_Read_Their_Own_Record()
        {
            var member = await SeedUserAsync(UserRole.Member);
            using var memberClient = await ClientAsAsync(member.Email, DefaultPassword);

            var body = await ReadAsync<UserDTO>(await memberClient.GetAsync($"/api/users/{member.Id}"));

            Assert.True(body.Success);
            Assert.Equal(member.Id, body.Data!.Id);
        }

        [Fact]
        public async Task A_Member_Cannot_Read_Someone_Elses_Record()
        {
            var member = await SeedUserAsync(UserRole.Member);
            var other = await SeedUserAsync(UserRole.Member);

            using var memberClient = await ClientAsAsync(member.Email, DefaultPassword);

            var body = await ReadAsync<UserDTO>(await memberClient.GetAsync($"/api/users/{other.Id}"));

            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.Forbidden, body.StatusCode);
        }

        [Fact]
        public async Task The_Admin_Can_Read_Anyones_Record()
        {
            var member = await SeedUserAsync(UserRole.Member);
            using var admin = await AdminClientAsync();

            var body = await ReadAsync<UserDTO>(await admin.GetAsync($"/api/users/{member.Id}"));

            Assert.True(body.Success);
            Assert.Equal(member.Id, body.Data!.Id);
        }

        [Fact]
        public async Task A_User_Can_Update_Their_Own_Profile()
        {
            var member = await SeedUserAsync(UserRole.Member);
            using var memberClient = await ClientAsAsync(member.Email, DefaultPassword);

            var newName = UniqueName("Renamed");

            var body = await ReadAsync<UserDTO>(await memberClient.PutAsJsonAsync($"/api/users/{member.Id}", new
            {
                name = newName,
                profileImageUrl = "https://example.test/avatar.png"
            }));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.Equal(newName, body.Data!.Name);
        }

        [Fact]
        public async Task A_User_Cannot_Update_Someone_Elses_Profile()
        {
            var member = await SeedUserAsync(UserRole.Member);
            var other = await SeedUserAsync(UserRole.Member);

            using var memberClient = await ClientAsAsync(member.Email, DefaultPassword);

            var response = await memberClient.PutAsJsonAsync($"/api/users/{other.Id}", new { name = "Hijacked" });
            var body = await ReadAsync<UserDTO>(response);

            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.Forbidden, body.StatusCode);
        }

        [Fact]
        public async Task Changing_A_Role_Is_Admin_Only()
        {
            var member = await SeedUserAsync(UserRole.Member);
            using var memberClient = await ClientAsAsync(member.Email, DefaultPassword);

            var response = await memberClient.PutAsJsonAsync($"/api/users/{member.Id}/role", new { role = nameof(UserRole.Admin) });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task The_Admin_Can_Promote_A_Member_To_Manager()
        {
            var member = await SeedUserAsync(UserRole.Member);
            using var admin = await AdminClientAsync();

            var body = await ReadAsync<UserDTO>(
                await admin.PutAsJsonAsync($"/api/users/{member.Id}/role", new { role = nameof(UserRole.Manager) }));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.Equal(UserRole.Manager, body.Data!.Role);
        }

        [Fact]
        public async Task The_Last_Admin_Cannot_Be_Demoted()
        {
            using var admin = await AdminClientAsync();
            var me = await ReadAsync<UserDTO>(await admin.GetAsync("/api/auth/me"));

            var body = await ReadAsync<UserDTO>(
                await admin.PutAsJsonAsync($"/api/users/{me.Data!.Id}/role", new { role = nameof(UserRole.Member) }));

            // Register is Admin-only, so losing the last Admin would lock the
            // whole app out of ever creating an account again.
            Assert.False(body.Success);
            Assert.Contains(body.Errors, error => error.Contains("last active Admin", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task The_Last_Admin_Cannot_Be_Disabled()
        {
            using var admin = await AdminClientAsync();
            var me = await ReadAsync<UserDTO>(await admin.GetAsync("/api/auth/me"));

            var body = await ReadAsync<UserDTO>(
                await admin.PutAsJsonAsync($"/api/users/{me.Data!.Id}/status", new { isDisabled = true }));

            Assert.False(body.Success);
            Assert.Contains(body.Errors, error => error.Contains("last active Admin", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Deleting_A_User_Disables_Them_Rather_Than_Removing_The_Row()
        {
            var member = await SeedUserAsync(UserRole.Member);
            using var admin = await AdminClientAsync();

            var deleted = await ReadAsync<UserDTO>(await admin.DeleteAsync($"/api/users/{member.Id}"));
            Assert.True(deleted.Success, string.Join("; ", deleted.Errors));

            // Still there, just switched off — their tasks, comments and
            // activity history depend on the row surviving.
            var stillThere = await ReadAsync<UserDTO>(await admin.GetAsync($"/api/users/{member.Id}"));
            Assert.True(stillThere.Success);
            Assert.True(stillThere.Data!.IsDisabled);
        }

        [Fact]
        public async Task A_Disabled_Account_Cannot_Log_In()
        {
            var member = await SeedUserAsync(UserRole.Member);
            using var admin = await AdminClientAsync();

            await admin.DeleteAsync($"/api/users/{member.Id}");

            using var client = CreateClient();
            var body = await ReadAsync<AuthResponseDto>(await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = member.Email,
                password = DefaultPassword,
                rememberMe = false
            }));

            Assert.False(body.Success);
        }

        [Fact]
        public async Task Logging_In_Marks_Today_Present_On_The_Attendance_Calendar()
        {
            var member = await SeedUserAsync(UserRole.Member);

            // The login itself is what writes the attendance row.
            using var memberClient = await ClientAsAsync(member.Email, DefaultPassword);

            var body = await ReadAsync<AttendanceSummaryDto>(
                await memberClient.GetAsync($"/api/users/{member.Id}/attendance"));

            Assert.True(body.Success, string.Join("; ", body.Errors));

            var today = body.Data!.Days.SingleOrDefault(day => day.Date == DateOnly.FromDateTime(DateTime.UtcNow));
            Assert.NotNull(today);
            Assert.True(today!.IsPresent);
            Assert.NotNull(today.FirstLoginAt);
            Assert.True(body.Data.DaysPresent >= 1);
        }

        [Fact]
        public async Task A_Member_Cannot_See_Someone_Elses_Attendance()
        {
            var member = await SeedUserAsync(UserRole.Member);
            var other = await SeedUserAsync(UserRole.Member);

            using var memberClient = await ClientAsAsync(member.Email, DefaultPassword);

            var body = await ReadAsync<AttendanceSummaryDto>(
                await memberClient.GetAsync($"/api/users/{other.Id}/attendance"));

            Assert.False(body.Success);
            Assert.Equal(HttpStatusCode.Forbidden, body.StatusCode);
        }
    }
}

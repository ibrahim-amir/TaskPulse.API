using System.Net;
using System.Net.Http.Json;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Tests.Infrastructure;

namespace TaskPulse.API.Tests
{
    public class AuthTests : ApiTestBase
    {
        public AuthTests(TaskPulseApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Seeded_Admin_Can_Log_In()
        {
            using var client = CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = AdminEmail,
                password = AdminPassword,
                rememberMe = false
            });

            var body = await ReadAsync<AuthResponseDto>(response);

            Assert.True(body.Success);
            Assert.False(string.IsNullOrWhiteSpace(body.Data!.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(body.Data.RefreshToken));
        }

        [Fact]
        public async Task Login_With_Wrong_Password_Is_Rejected()
        {
            using var client = CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = AdminEmail,
                password = "NotThePassword1!",
                rememberMe = false
            });

            var body = await ReadAsync<AuthResponseDto>(response);

            Assert.False(body.Success);
            Assert.Null(body.Data);
        }

        [Fact]
        public async Task Login_With_Unknown_Email_Gives_The_Same_Answer_As_A_Wrong_Password()
        {
            using var client = CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "nobody@test.local",
                password = "Password123!",
                rememberMe = false
            });

            var body = await ReadAsync<AuthResponseDto>(response);

            // Same wording either way, so this can't be used to find out which
            // emails are registered.
            Assert.False(body.Success);
            Assert.Contains("Invalid email or password.", body.Errors);
        }

        [Fact]
        public async Task Me_Returns_The_Caller_With_Their_Role()
        {
            using var admin = await AdminClientAsync();

            var body = await ReadAsync<UserDTO>(await admin.GetAsync("/api/auth/me"));

            Assert.True(body.Success);
            Assert.Equal(AdminEmail, body.Data!.Email);
            Assert.Equal(UserRole.Admin, body.Data.Role);
        }

        [Fact]
        public async Task Me_Without_A_Token_Is_Unauthorised()
        {
            using var client = CreateClient();

            var response = await client.GetAsync("/api/auth/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Register_Without_A_Token_Is_Unauthorised()
        {
            using var client = CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/register", NewUserPayload());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Register_As_A_Member_Is_Forbidden()
        {
            var member = await SeedUserAsync(UserRole.Member);
            using var memberClient = await ClientAsAsync(member.Email, DefaultPassword);

            var response = await memberClient.PostAsJsonAsync("/api/auth/register", NewUserPayload());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Register_As_Admin_Creates_The_Account()
        {
            using var admin = await AdminClientAsync();
            var payload = NewUserPayload(UserRole.Manager);

            var body = await ReadAsync<UserDTO>(await admin.PostAsJsonAsync("/api/auth/register", payload));

            Assert.True(body.Success, string.Join("; ", body.Errors));
            Assert.Equal(UserRole.Manager, body.Data!.Role);
            Assert.False(body.Data.IsDisabled);
        }

        [Fact]
        public async Task Registered_User_Response_Never_Carries_A_Password()
        {
            using var admin = await AdminClientAsync();

            var response = await admin.PostAsJsonAsync("/api/auth/register", NewUserPayload());
            var raw = await response.Content.ReadAsStringAsync();

            // The stray empty "password" field that live testing turned up.
            Assert.DoesNotContain("\"password\"", raw, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Email_Without_A_Domain_Is_Rejected()
        {
            using var admin = await AdminClientAsync();

            // "name@gmail" with no .com — accepted before the validator was
            // switched to the stricter mode.
            var payload = NewUserPayload();
            payload["email"] = "someone@gmail";

            var body = await ReadAsync<UserDTO>(await admin.PostAsJsonAsync("/api/auth/register", payload));

            Assert.False(body.Success);
            Assert.Contains(body.Errors, error => error.Contains("email", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Mismatched_Confirm_Password_Is_Rejected()
        {
            using var admin = await AdminClientAsync();

            var payload = NewUserPayload();
            payload["confirmPassword"] = "SomethingElse1!";

            var body = await ReadAsync<UserDTO>(await admin.PostAsJsonAsync("/api/auth/register", payload));

            Assert.False(body.Success);
        }

        [Fact]
        public async Task A_New_User_Cannot_Log_In_Until_They_Confirm_Their_Email()
        {
            using var admin = await AdminClientAsync();

            var payload = NewUserPayload();
            var email = (string)payload["email"];
            var password = (string)payload["password"];

            Assert.True((await ReadAsync<UserDTO>(await admin.PostAsJsonAsync("/api/auth/register", payload))).Success);

            using var client = CreateClient();
            var blocked = await ReadAsync<AuthResponseDto>(
                await client.PostAsJsonAsync("/api/auth/login", new { email, password, rememberMe = false }));

            Assert.False(blocked.Success);

            // Confirm with the token the notifier "emailed", then it works.
            var token = Factory.Emails.LastTokenFor(email);
            Assert.False(string.IsNullOrWhiteSpace(token));

            var confirmed = await ReadAsync<string>(
                await client.PostAsJsonAsync("/api/auth/confirm-email", new { email, token }));
            Assert.True(confirmed.Success, string.Join("; ", confirmed.Errors));

            var allowed = await ReadAsync<AuthResponseDto>(
                await client.PostAsJsonAsync("/api/auth/login", new { email, password, rememberMe = false }));
            Assert.True(allowed.Success, string.Join("; ", allowed.Errors));
        }

        [Fact]
        public async Task Refresh_Returns_A_New_Token_Pair()
        {
            using var client = CreateClient();

            var login = await ReadAsync<AuthResponseDto>(await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = AdminEmail,
                password = AdminPassword,
                rememberMe = false
            }));

            var refreshed = await ReadAsync<AuthResponseDto>(await client.PostAsJsonAsync("/api/auth/refresh", new
            {
                refreshToken = login.Data!.RefreshToken
            }));

            Assert.True(refreshed.Success, string.Join("; ", refreshed.Errors));
            Assert.NotEqual(login.Data.RefreshToken, refreshed.Data!.RefreshToken);
        }

        [Fact]
        public async Task A_Refresh_Token_Stops_Working_After_Logout()
        {
            var user = await SeedUserAsync(UserRole.Member);

            using var client = CreateClient();
            var login = await ReadAsync<AuthResponseDto>(await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = user.Email,
                password = DefaultPassword,
                rememberMe = false
            }));

            using var authed = await ClientAsAsync(user.Email, DefaultPassword);
            Assert.True((await ReadAsync<string>(await authed.PostAsync("/api/auth/logout", null))).Success);

            var afterLogout = await ReadAsync<AuthResponseDto>(await client.PostAsJsonAsync("/api/auth/refresh", new
            {
                refreshToken = login.Data!.RefreshToken
            }));

            Assert.False(afterLogout.Success);
        }

        [Fact]
        public async Task Forgot_Password_Says_The_Same_Thing_For_Real_And_Unknown_Emails()
        {
            using var client = CreateClient();

            var known = await ReadAsync<string>(
                await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = AdminEmail }));

            var unknown = await ReadAsync<string>(
                await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "nobody@test.local" }));

            Assert.True(known.Success);
            Assert.True(unknown.Success);
            Assert.Equal(known.Messages, unknown.Messages);
        }

        [Fact]
        public async Task Password_Can_Be_Reset_With_The_Emailed_Token()
        {
            var user = await SeedUserAsync(UserRole.Member);
            const string newPassword = "BrandNew123!";

            using var client = CreateClient();
            await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = user.Email });

            var token = Factory.Emails.LastTokenFor(user.Email);
            Assert.False(string.IsNullOrWhiteSpace(token));

            var reset = await ReadAsync<string>(await client.PostAsJsonAsync("/api/auth/reset-password", new
            {
                email = user.Email,
                token,
                newPassword,
                confirmNewPassword = newPassword
            }));
            Assert.True(reset.Success, string.Join("; ", reset.Errors));

            var withNew = await ReadAsync<AuthResponseDto>(await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = user.Email,
                password = newPassword,
                rememberMe = false
            }));
            Assert.True(withNew.Success);

            var withOld = await ReadAsync<AuthResponseDto>(await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = user.Email,
                password = DefaultPassword,
                rememberMe = false
            }));
            Assert.False(withOld.Success);
        }

        private static Dictionary<string, object> NewUserPayload(UserRole role = UserRole.Member)
        {
            var unique = Guid.NewGuid().ToString("N")[..8];

            return new Dictionary<string, object>
            {
                ["name"] = $"Test User {unique}",
                ["email"] = $"user-{unique}@test.local",
                ["password"] = "Password123!",
                ["confirmPassword"] = "Password123!",
                ["role"] = role.ToString()
            };
        }
    }
}

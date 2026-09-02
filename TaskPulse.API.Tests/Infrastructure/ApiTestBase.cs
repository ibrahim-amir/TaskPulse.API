using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.Data;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.PasswordHelperClass;

namespace TaskPulse.API.Tests.Infrastructure
{
    // Shared plumbing for every test class: logging in, building an
    // authenticated client, reading the CommonResponse envelope, and putting
    // users straight into the database when a test just needs someone to exist.
    // Scenario setup (a project with a team, a task with a checklist) lives in
    // ApiTestBase.Scenarios.cs so this file stays about plumbing.
    [Collection(ApiCollection.Name)]
    public abstract partial class ApiTestBase
    {
        // The account the AddUserRole migration seeds. Every test that needs to
        // do something privileged starts here.
        protected const string AdminEmail = "admin@taskpulse.local";
        protected const string AdminPassword = "ChangeMe123!";
        protected const string DefaultPassword = "Password123!";

        protected readonly TaskPulseApiFactory Factory;

        protected ApiTestBase(TaskPulseApiFactory factory)
        {
            Factory = factory;
            Factory.EnsureDatabase();
        }

        // Matches how the API serialises: camelCase properties, enums as
        // strings. Without this every enum comes back unreadable.
        protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        protected HttpClient CreateClient() => Factory.CreateClient();

        protected async Task<HttpClient> ClientAsAsync(string email, string password)
        {
            var token = await GetTokenAsync(email, password);
            var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        protected Task<HttpClient> AdminClientAsync() => ClientAsAsync(AdminEmail, AdminPassword);

        protected async Task<string> GetTokenAsync(string email, string password)
        {
            using var client = Factory.CreateClient();
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password, rememberMe = false });

            var body = await ReadAsync<AuthResponseDto>(response);

            Assert.True(body.Success, $"Login failed for {email}: {string.Join("; ", body.Errors)}");
            Assert.NotNull(body.Data);

            return body.Data!.AccessToken;
        }

        // Reads the CommonResponse envelope every endpoint returns.
        protected static async Task<CommonResponse<T>> ReadAsync<T>(HttpResponseMessage response)
        {
            var raw = await response.Content.ReadAsStringAsync();

            var parsed = JsonSerializer.Deserialize<CommonResponse<T>>(raw, Json);
            Assert.NotNull(parsed);

            return parsed!;
        }

        // Straight into the database, bypassing register and email confirmation.
        // Those flows have their own tests; everything else just needs a user
        // that exists and can log in.
        protected async Task<User> SeedUserAsync(UserRole role, string password = DefaultPassword)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var unique = Guid.NewGuid().ToString("N")[..8];

            var user = new User
            {
                Name = $"{role} {unique}",
                Email = $"{role.ToString().ToLowerInvariant()}-{unique}@test.local",
                PasswordHash = PasswordHelper.HashPassword(password),
                Role = role,
                IsEmailConfirmed = true
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return user;
        }

        // Unique names keep tests independent — project names are unique, and
        // tests share one database for the run. Only trim when there's
        // something to trim: a short prefix leaves this under 40 characters,
        // and slicing past the end throws.
        protected static string UniqueName(string prefix)
        {
            var name = $"{prefix} {Guid.NewGuid():N}";
            return name.Length <= 40 ? name : name[..40];
        }

        protected async Task<T?> QueryAsync<T>(Func<AppDbContext, Task<T?>> query)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await query(db);
        }

        protected async Task<TaskItem?> GetTaskFromDatabaseAsync(int taskId)
        {
            return await QueryAsync(db => db.Tasks
                .Include(t => t.Subtasks)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId));
        }
    }

    // One factory for the whole run: booting the host per test class would add
    // seconds for no benefit.
    [CollectionDefinition(Name)]
    public class ApiCollection : ICollectionFixture<TaskPulseApiFactory>
    {
        public const string Name = "TaskPulse API";
    }
}

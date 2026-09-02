using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskPulse.API.Data;
using TaskPulse.API.Services.BackgroundServices;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Tests.Infrastructure
{
    // Boots the real API in memory — the real pipeline, real JWT auth, real EF
    // Core, real controllers — and points it at a throwaway database.
    //
    // The only things swapped out are the two that would make tests unreliable:
    // the email sender (so tokens can be read back instead of going to a log)
    // and the deadline monitor (a timer firing mid-test is noise).
    public class TaskPulseApiFactory : WebApplicationFactory<Program>
    {
        // Same LocalDB instance as the app, a different database. Tests drop and
        // recreate this on every run, so it must never be the real one.
        public const string TestConnectionString =
            "Server=(localdb)\\mssqllocaldb;Database=TaskPulseDb_Tests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

        private static bool _databaseReady;
        private static readonly object DatabaseGate = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = TestConnectionString,

                    // A fixed key so tokens are valid for the whole run, and long
                    // enough for HMAC-SHA256 to accept it.
                    ["Jwt:Key"] = "taskpulse-integration-test-signing-key-please-do-not-use-in-production",

                    // Keep uploads out of the app's own folder.
                    ["FileUpload:StorageRoot"] = "test-uploads"
                });
            });

            builder.ConfigureServices(services =>
            {
                var monitor = services.SingleOrDefault(d => d.ImplementationType == typeof(TaskDeadlineMonitor));
                if (monitor is not null)
                    services.Remove(monitor);

                services.RemoveAll<IEmailSender>();
                services.AddSingleton<TestEmailSender>();
                services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<TestEmailSender>());
            });
        }

        // Dropped and rebuilt once per test run, not per test — rebuilding a
        // SQL Server database for every test would take longer than the tests.
        // EnsureCreated also applies the seeded Admin from HasData.
        public void EnsureDatabase()
        {
            lock (DatabaseGate)
            {
                if (_databaseReady)
                    return;

                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                _databaseReady = true;
            }
        }

        public TestEmailSender Emails => Services.GetRequiredService<TestEmailSender>();
    }
}

using Microsoft.EntityFrameworkCore;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.PasswordHelperClass;

namespace TaskPulse.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

            // Single hardcoded Admin, per the single-org app design. Login with
            // this account, then change the password via forgot/reset-password.
            e.HasData(new User
            {
                Id = 1,
                Name = "Admin",
                Email = "admin@taskpulse.local",
                PasswordHash = PasswordHelper.HashPassword("ChangeMe123!"),
                Role = UserRole.Admin,
                IsEmailConfirmed = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Priority).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<TaskItem>(e =>
        {
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.EstimatedHours).HasColumnType("decimal(6,2)");
            e.Property(t => t.ActualHours).HasColumnType("decimal(6,2)");

            e.HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectMember>(e =>
        {
            e.HasIndex(pm => new { pm.ProjectId, pm.UserId }).IsUnique();
            e.Property(pm => pm.Role).HasConversion<string>().HasMaxLength(20);

            e.HasOne(pm => pm.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskAssignment>(e =>
        {
            e.HasIndex(a => new { a.TaskId, a.UserId }).IsUnique();
            e.Property(a => a.AssignedByRole).HasConversion<string>().HasMaxLength(20);

            e.HasOne(a => a.Task)
                .WithMany(t => t.Assignments)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.User)
                .WithMany(u => u.TaskAssignments)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(e =>
        {
            e.HasOne(c => c.Task)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaskAttachment>(e =>
        {
            e.HasOne(a => a.Task)
                .WithMany(t => t.Attachments)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.UploadedByUser)
                .WithMany(u => u.UploadedAttachments)
                .HasForeignKey(a => a.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActivityLog>(e =>
        {
            e.HasOne(a => a.Project)
                .WithMany()
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            e.HasOne(a => a.Task)
                .WithMany()
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }
}

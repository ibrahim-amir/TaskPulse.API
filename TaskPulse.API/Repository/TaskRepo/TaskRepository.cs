using Microsoft.EntityFrameworkCore;
using TaskPulse.API.Data;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.PagingHelper;
using TaskPulse.API.Repository.Infrastructure;

namespace TaskPulse.API.Repository.TaskRepo
{
    // A task owns its assignments, its required-file checklist and its
    // attachments — none of them mean anything on their own — so they share a
    // repository, the same way ProjectMembers sit with Projects.
    public class TaskRepository : BaseRepositoryClass
    {
        public TaskRepository(AppDbContext db) : base(db) { }

        // Everything a TaskDTO needs, so callers don't each remember the
        // Include list.
        private IQueryable<TaskItem> TasksWithDetail()
        {
            return _db.Tasks
                .Include(t => t.Assignments)
                .Include(t => t.Subtasks)
                    .ThenInclude(s => s.Attachment);
        }

        public async Task CreateAsync(TaskItem task)
        {
            await _db.Tasks.AddAsync(task);
            await _db.SaveChangesAsync();
        }

        public async Task<TaskItem?> GetByIdAsync(int id)
        {
            return await TasksWithDetail().FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task UpdateAsync(TaskItem task)
        {
            task.UpdatedAt = DateTime.UtcNow;
            _db.Tasks.Update(task);
            await _db.SaveChangesAsync();
        }

        // Same reason as ProjectRepository.DeleteAsync: the activity rows point
        // at this task with ClientSetNull, so they have to be cleared here or
        // the database refuses the delete. The history outlives the task.
        public async Task DeleteAsync(TaskItem task)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var logs = await _db.ActivityLogs
                .Where(a => a.TaskId == task.Id)
                .ToListAsync();

            foreach (var log in logs)
                log.TaskId = null;

            // Subtasks reference attachments with NoAction and both are
            // cascade-deleted with the task — break the link before the cascade.
            var subtasks = await _db.TaskSubtasks
                .Where(s => s.TaskId == task.Id)
                .ToListAsync();

            foreach (var subtask in subtasks)
                subtask.AttachmentId = null;

            await _db.SaveChangesAsync();

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        // projectId null means "across every project the caller can see", which
        // is what the dashboard and the personal task list use.
        // visibleToUserId null means no scoping — the Admin.
        public async Task<PagedResult<TaskItem>> GetPagedAsync(TaskQueryDto query, int? projectId, int? visibleToUserId)
        {
            var tasks = TasksWithDetail().AsNoTracking();

            if (projectId.HasValue)
                tasks = tasks.Where(t => t.ProjectId == projectId.Value);

            if (visibleToUserId.HasValue)
                tasks = tasks.Where(t => t.Project.Members.Any(m => m.UserId == visibleToUserId.Value));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                tasks = tasks.Where(t => t.Title.Contains(search)
                                      || (t.Description != null && t.Description.Contains(search)));
            }

            if (query.Status.HasValue)
                tasks = tasks.Where(t => t.Status == query.Status.Value);

            if (query.Priority.HasValue)
                tasks = tasks.Where(t => t.Priority == query.Priority.Value);

            if (query.AssignedUserId.HasValue)
                tasks = tasks.Where(t => t.Assignments.Any(a => a.UserId == query.AssignedUserId.Value));

            if (query.OverdueOnly == true)
            {
                var now = DateTime.UtcNow;
                tasks = tasks.Where(t => t.DueDate != null && t.DueDate < now && t.Status != TaskItemStatus.Done);
            }

            return await tasks
                .ApplySorting(query.SortBy ?? nameof(TaskItem.Id), query.SortOrder)
                .ToPagedResultAsync(query);
        }

        // --- Assignments ---

        public async Task ReplaceAssignmentsAsync(TaskItem task, List<int> userIds, UserRole assignedByRole)
        {
            var existing = await _db.TaskAssignments.Where(a => a.TaskId == task.Id).ToListAsync();
            _db.TaskAssignments.RemoveRange(existing);

            var assignments = userIds.Distinct().Select(userId => new TaskAssignment
            {
                TaskId = task.Id,
                UserId = userId,
                AssignedByRole = assignedByRole
            });

            await _db.TaskAssignments.AddRangeAsync(assignments);
            await _db.SaveChangesAsync();
        }

        public async Task<List<int>> GetAssigneeIdsAsync(int taskId)
        {
            return await _db.TaskAssignments
                .Where(a => a.TaskId == taskId)
                .Select(a => a.UserId)
                .ToListAsync();
        }

        public async Task<bool> IsAssignedAsync(int taskId, int userId)
        {
            return await _db.TaskAssignments.AnyAsync(a => a.TaskId == taskId && a.UserId == userId);
        }

        // --- Subtasks (the required-file checklist) ---

        public async Task AddSubtaskAsync(TaskSubtask subtask)
        {
            await _db.TaskSubtasks.AddAsync(subtask);
            await _db.SaveChangesAsync();
        }

        public async Task AddSubtasksAsync(IEnumerable<TaskSubtask> subtasks)
        {
            await _db.TaskSubtasks.AddRangeAsync(subtasks);
            await _db.SaveChangesAsync();
        }

        public async Task<TaskSubtask?> GetSubtaskAsync(int subtaskId)
        {
            return await _db.TaskSubtasks
                .Include(s => s.Attachment)
                .Include(s => s.Task)
                .FirstOrDefaultAsync(s => s.Id == subtaskId);
        }

        public async Task<List<TaskSubtask>> GetSubtasksAsync(int taskId)
        {
            return await _db.TaskSubtasks
                .AsNoTracking()
                .Include(s => s.Attachment)
                .Where(s => s.TaskId == taskId)
                .ToListAsync();
        }

        public async Task UpdateSubtaskAsync(TaskSubtask subtask)
        {
            _db.TaskSubtasks.Update(subtask);
            await _db.SaveChangesAsync();
        }

        public async Task RemoveSubtaskAsync(TaskSubtask subtask)
        {
            _db.TaskSubtasks.Remove(subtask);
            await _db.SaveChangesAsync();
        }

        // True once every required file on the task has been uploaded — what
        // flips the task into Review.
        public async Task<bool> AllSubtasksUploadedAsync(int taskId)
        {
            var total = await _db.TaskSubtasks.CountAsync(s => s.TaskId == taskId);
            if (total == 0)
                return false;

            var uploaded = await _db.TaskSubtasks.CountAsync(s => s.TaskId == taskId && s.IsUploaded);
            return uploaded == total;
        }

        // --- Attachments ---

        public async Task AddAttachmentAsync(TaskAttachment attachment)
        {
            await _db.TaskAttachments.AddAsync(attachment);
            await _db.SaveChangesAsync();
        }

        public async Task<TaskAttachment?> GetAttachmentAsync(int attachmentId)
        {
            return await _db.TaskAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId);
        }

        public async Task<List<TaskAttachment>> GetAttachmentsAsync(int taskId)
        {
            return await _db.TaskAttachments
                .AsNoTracking()
                .Where(a => a.TaskId == taskId)
                .ToListAsync();
        }

        public async Task RemoveAttachmentAsync(TaskAttachment attachment)
        {
            _db.TaskAttachments.Remove(attachment);
            await _db.SaveChangesAsync();
        }
    }
}

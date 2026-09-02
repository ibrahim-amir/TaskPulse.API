using Microsoft.EntityFrameworkCore;
using TaskPulse.API.Data;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.ProjectDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Helper.PagingHelper;
using TaskPulse.API.Repository.Infrastructure;

namespace TaskPulse.API.Repository.ProjectRepo
{
    // Projects and their membership are one aggregate — a ProjectMember row
    // means nothing without its project — so they share a repository.
    public class ProjectRepository : BaseRepositoryClass
    {
        public ProjectRepository(AppDbContext db) : base(db) { }

        public async Task CreateAsync(Project project)
        {
            await _db.Projects.AddAsync(project);
            await _db.SaveChangesAsync();
        }

        public async Task<Project?> GetByIdAsync(int id)
        {
            return await _db.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task UpdateAsync(Project project)
        {
            _db.Projects.Update(project);
            await _db.SaveChangesAsync();
        }

        // ActivityLog's links to Project and Task are ClientSetNull, which only
        // clears rows EF has actually loaded into the change tracker. Nothing
        // loads them, so the database still sees live references and blocks the
        // delete. Clearing them here is what makes ClientSetNull work as
        // intended: the audit trail survives the project it described.
        public async Task DeleteAsync(Project project)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var taskIds = await _db.Tasks
                .Where(t => t.ProjectId == project.Id)
                .Select(t => t.Id)
                .ToListAsync();

            var logs = await _db.ActivityLogs
                .Where(a => a.ProjectId == project.Id || (a.TaskId != null && taskIds.Contains(a.TaskId.Value)))
                .ToListAsync();

            foreach (var log in logs)
            {
                log.ProjectId = null;
                log.TaskId = null;
            }

            // Subtasks point at attachments with NoAction, and the task cascade
            // deletes both tables — break that link first so the order the
            // database happens to pick can't matter.
            var subtasks = await _db.TaskSubtasks
                .Where(s => taskIds.Contains(s.TaskId))
                .ToListAsync();

            foreach (var subtask in subtasks)
                subtask.AttachmentId = null;

            await _db.SaveChangesAsync();

            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        // visibleToUserId null means "no scoping" — that's the Admin, who sees
        // everything. Anyone else only sees projects they're a member of.
        public async Task<PagedResult<Project>> GetPagedAsync(ProjectQueryDto query, int? visibleToUserId)
        {
            var projects = _db.Projects
                .AsNoTracking()
                .Include(p => p.Members);

            IQueryable<Project> filtered = projects;

            if (visibleToUserId.HasValue)
                filtered = filtered.Where(p => p.Members.Any(m => m.UserId == visibleToUserId.Value));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                filtered = filtered.Where(p => p.Name.Contains(search)
                                            || (p.Description != null && p.Description.Contains(search)));
            }

            if (query.Status.HasValue)
                filtered = filtered.Where(p => p.Status == query.Status.Value);

            if (query.Priority.HasValue)
                filtered = filtered.Where(p => p.Priority == query.Priority.Value);

            return await filtered
                .ApplySorting(query.SortBy ?? nameof(Project.Id), query.SortOrder)
                .ToPagedResultAsync(query);
        }

        public async Task<bool> NameExistsAsync(string name, int? excludingProjectId = null)
        {
            return await _db.Projects
                .AnyAsync(p => p.Name == name && (excludingProjectId == null || p.Id != excludingProjectId.Value));
        }

        // --- Membership ---

        public async Task<List<ProjectMember>> GetMembersAsync(int projectId)
        {
            return await _db.ProjectMembers
                .AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.ProjectId == projectId)
                .ToListAsync();
        }

        public async Task<ProjectMember?> GetMemberAsync(int projectId, int userId)
        {
            return await _db.ProjectMembers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId);
        }

        // By the membership row's own id — an approved removal stored that id
        // as its target, so it doesn't need the project/user pair again.
        public async Task<ProjectMember?> GetMemberByIdAsync(int memberId)
        {
            return await _db.ProjectMembers
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == memberId);
        }

        public async Task AddMemberAsync(ProjectMember member)
        {
            await _db.ProjectMembers.AddAsync(member);
            await _db.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(ProjectMember member)
        {
            _db.ProjectMembers.Remove(member);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> IsMemberAsync(int projectId, int userId)
        {
            return await _db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId);
        }

        // The projects this user is on. Used to scope the activity feed and the
        // dashboard to what they're allowed to see.
        public async Task<List<int>> GetProjectIdsForUserAsync(int userId)
        {
            return await _db.ProjectMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.ProjectId)
                .ToListAsync();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using TaskPulse.API.Data;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.PagingHelper;
using TaskPulse.API.Helper.PasswordHelperClass;
using TaskPulse.API.Repository.Infrastructure;

namespace TaskPulse.API.Repository.UserRepo
{
    public class UserRepository : BaseRepositoryClass
    {
        public UserRepository(AppDbContext db) : base(db) { }

        public async Task CreateUserAsync(User user)
        {
            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        // Bulk lookup for the busy-assignment gate — checking N assignees'
        // availability one at a time would be N round trips.
        public async Task<List<User>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            return await _db.Users.AsNoTracking().Where(u => idList.Contains(u.Id)).ToListAsync();
        }

        public async Task<bool> CheckPasswordByIdAsync(int id, string password)
        {
            var user = await _db.Users.FindAsync(id);
            return user is not null && PasswordHelper.VerifyPassword(password, user.PasswordHash);
        }

        public async Task<bool> CheckPasswordByEmailAsync(string email, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user is not null && PasswordHelper.VerifyPassword(password, user.PasswordHash);
        }

        public async Task<User?> GetByRefreshTokenHashAsync(string refreshTokenHash)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.RefreshTokenHash == refreshTokenHash);
        }

        public async Task UpdateUserAsync(User user)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }

        // Filters first, then sorting and paging from the shared helpers.
        // Sorts by Id when the caller didn't ask for anything, so pages stay
        // in a stable order instead of whatever the database returns.
        public async Task<PagedResult<User>> GetPagedAsync(UserQueryDto query)
        {
            var users = _db.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                users = users.Where(u => u.Name.Contains(search) || u.Email.Contains(search));
            }

            if (query.Role.HasValue)
                users = users.Where(u => u.Role == query.Role.Value);

            if (query.IsDisabled.HasValue)
                users = users.Where(u => u.IsDisabled == query.IsDisabled.Value);

            return await users
                .ApplySorting(query.SortBy ?? nameof(User.Id), query.SortOrder)
                .ToPagedResultAsync(query);
        }

        // Used before demoting or disabling an Admin — the app can't create
        // accounts at all without one, so the last one can't be removed.
        public async Task<int> CountActiveAdminsAsync()
        {
            return await _db.Users.CountAsync(u => u.Role == UserRole.Admin && !u.IsDisabled);
        }

        // Everything that would either block a hard delete outright (these
        // relationships are Restrict, not Cascade — SQL would reject the delete)
        // or quietly take other people's data with it. Checked up front so
        // DeleteAsync only ever runs when it's actually safe.
        public async Task<List<string>> GetDeletionBlockersAsync(int userId)
        {
            var blockers = new List<string>();

            var projectsCreated = await _db.Projects.CountAsync(p => p.CreatedByUserId == userId);
            if (projectsCreated > 0)
                blockers.Add($"created {projectsCreated} project(s)");

            var tasksCreated = await _db.Tasks.CountAsync(t => t.CreatedByUserId == userId);
            if (tasksCreated > 0)
                blockers.Add($"created {tasksCreated} task(s)");

            var comments = await _db.Comments.CountAsync(c => c.UserId == userId);
            if (comments > 0)
                blockers.Add($"posted {comments} comment(s)");

            var uploads = await _db.TaskAttachments.CountAsync(a => a.UploadedByUserId == userId);
            if (uploads > 0)
                blockers.Add($"uploaded {uploads} file(s)");

            var approvals = await _db.ApprovalRequests
                .CountAsync(a => a.RequestedByUserId == userId || a.ResolvedByUserId == userId);
            if (approvals > 0)
                blockers.Add($"{approvals} approval record(s)");

            return blockers;
        }

        public async Task DeleteAsync(User user)
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }
    }
}

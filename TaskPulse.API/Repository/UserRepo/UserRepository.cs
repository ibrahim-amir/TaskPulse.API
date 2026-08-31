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
    }
}

using Microsoft.EntityFrameworkCore;
using TaskPulse.API.Data;
using TaskPulse.API.Entities;
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
    }
}

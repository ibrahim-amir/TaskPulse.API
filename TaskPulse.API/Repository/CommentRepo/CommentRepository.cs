using Microsoft.EntityFrameworkCore;
using TaskPulse.API.Data;
using TaskPulse.API.Entities;
using TaskPulse.API.Repository.Infrastructure;

namespace TaskPulse.API.Repository.CommentRepo
{
    public class CommentRepository : BaseRepositoryClass
    {
        public CommentRepository(AppDbContext db) : base(db) { }

        public async Task CreateAsync(Comment comment)
        {
            await _db.Comments.AddAsync(comment);
            await _db.SaveChangesAsync();
        }

        public async Task<Comment?> GetByIdAsync(int id)
        {
            return await _db.Comments
                .Include(c => c.User)
                .Include(c => c.Task)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // Oldest first — a task's comments read as a conversation.
        public async Task<List<Comment>> GetByTaskAsync(int taskId)
        {
            return await _db.Comments
                .AsNoTracking()
                .Include(c => c.User)
                .Where(c => c.TaskId == taskId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(Comment comment)
        {
            comment.UpdatedAt = DateTime.UtcNow;
            _db.Comments.Update(comment);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Comment comment)
        {
            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();
        }
    }
}

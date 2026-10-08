using Microsoft.EntityFrameworkCore;
using TaskPulse.API.Data;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Repository.Infrastructure;

namespace TaskPulse.API.Repository.TaskApprovalRepo
{
    public class TaskApprovalRequestRepository : BaseRepositoryClass
    {
        public TaskApprovalRequestRepository(AppDbContext db) : base(db) { }

        private IQueryable<TaskApprovalRequest> WithDetail()
        {
            return _db.TaskApprovalRequests
                .Include(r => r.Task)
                .Include(r => r.RequestedByUser)
                .Include(r => r.Responses).ThenInclude(response => response.ApproverUser);
        }

        public async Task CreateAsync(TaskApprovalRequest request)
        {
            await _db.TaskApprovalRequests.AddAsync(request);
            await _db.SaveChangesAsync();
        }

        public async Task<TaskApprovalRequest?> GetByIdAsync(int id)
        {
            return await WithDetail().FirstOrDefaultAsync(r => r.Id == id);
        }

        // The most recent request for a task — what the task detail page needs
        // to decide whether to offer "Request approval" or "Submit".
        public async Task<TaskApprovalRequest?> GetLatestForTaskAsync(int taskId)
        {
            return await WithDetail()
                .Where(r => r.TaskId == taskId)
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<TaskApprovalRequest?> GetPendingForTaskAsync(int taskId)
        {
            return await WithDetail().FirstOrDefaultAsync(r => r.TaskId == taskId && r.Status == ApprovalStatus.Pending);
        }

        // Every still-open request where this user is a tagged approver who
        // hasn't answered yet — what the Approvals page shows them.
        public async Task<List<TaskApprovalRequest>> GetPendingForApproverAsync(int userId)
        {
            return await WithDetail()
                .Where(r => r.Status == ApprovalStatus.Pending
                         && r.Responses.Any(resp => resp.ApproverUserId == userId && resp.Status == ApprovalStatus.Pending))
                .OrderByDescending(r => r.Id)
                .ToListAsync();
        }

        // Every request this user has filed, regardless of status — the
        // Approvals page's "Sent by me" tab, so they can track what they're
        // still waiting on.
        public async Task<List<TaskApprovalRequest>> GetRequestedByAsync(int userId)
        {
            return await WithDetail()
                .Where(r => r.RequestedByUserId == userId)
                .OrderByDescending(r => r.Id)
                .ToListAsync();
        }

        public async Task UpdateAsync(TaskApprovalRequest request)
        {
            _db.TaskApprovalRequests.Update(request);
            await _db.SaveChangesAsync();
        }
    }
}

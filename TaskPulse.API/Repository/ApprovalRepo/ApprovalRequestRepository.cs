using Microsoft.EntityFrameworkCore;
using TaskPulse.API.Data;
using TaskPulse.API.DTO.ApprovalDto;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.PagingHelper;
using TaskPulse.API.Repository.Infrastructure;

namespace TaskPulse.API.Repository.ApprovalRepo
{
    public class ApprovalRequestRepository : BaseRepositoryClass
    {
        public ApprovalRequestRepository(AppDbContext db) : base(db) { }

        public async Task CreateAsync(ApprovalRequest request)
        {
            await _db.ApprovalRequests.AddAsync(request);
            await _db.SaveChangesAsync();
        }

        public async Task<ApprovalRequest?> GetByIdAsync(int id)
        {
            return await _db.ApprovalRequests
                .Include(a => a.RequestedByUser)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task UpdateAsync(ApprovalRequest request)
        {
            _db.ApprovalRequests.Update(request);
            await _db.SaveChangesAsync();
        }

        public async Task<PagedResult<ApprovalRequest>> GetPagedAsync(ApprovalQueryDto query)
        {
            var requests = _db.ApprovalRequests
                .AsNoTracking()
                .Include(a => a.RequestedByUser)
                .AsQueryable();

            if (query.Status.HasValue)
                requests = requests.Where(a => a.Status == query.Status.Value);

            return await requests
                .ApplySorting(query.SortBy ?? nameof(ApprovalRequest.Id), query.SortOrder ?? "desc")
                .ToPagedResultAsync(query);
        }

        // Stops the same Manager stacking duplicate requests for one target
        // while an earlier one is still waiting.
        public async Task<ApprovalRequest?> GetPendingForTargetAsync(string targetEntityType, int targetEntityId)
        {
            return await _db.ApprovalRequests
                .FirstOrDefaultAsync(a => a.TargetEntityType == targetEntityType
                                       && a.TargetEntityId == targetEntityId
                                       && a.Status == ApprovalStatus.Pending);
        }
    }
}

using System.Text.Json;
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

        // Pending edit requests on tasks this user created — the creator's own
        // queue, separate from the Admin's (nobody but the creator resolves
        // these, so it can't reuse GetPagedAsync above).
        // Both edit and delete requests on tasks the caller created are
        // theirs to resolve, so this covers both action types.
        public async Task<List<ApprovalRequest>> GetPendingForTaskCreatorAsync(int creatorUserId)
        {
            return await _db.ApprovalRequests
                .AsNoTracking()
                .Include(a => a.RequestedByUser)
                .Where(a => (a.ActionType == ApprovalActionType.UpdateTask || a.ActionType == ApprovalActionType.DeleteTask)
                         && a.Status == ApprovalStatus.Pending
                         && _db.Tasks.Any(t => t.Id == a.TargetEntityId && t.CreatedByUserId == creatorUserId))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        // Same idea, for projects.
        public async Task<List<ApprovalRequest>> GetPendingForProjectCreatorAsync(int creatorUserId)
        {
            return await _db.ApprovalRequests
                .AsNoTracking()
                .Include(a => a.RequestedByUser)
                .Where(a => a.ActionType == ApprovalActionType.UpdateProject
                         && a.Status == ApprovalStatus.Pending
                         && _db.Projects.Any(p => p.Id == a.TargetEntityId && p.CreatedByUserId == creatorUserId))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        // Pending DuplicateTask requests the caller can resolve — every one
        // of them for an Admin, or just the ones whose source task belongs
        // to a project the caller manages otherwise.
        public async Task<List<ApprovalRequest>> GetPendingDuplicateRequestsForManagerAsync(int callerId, bool callerIsAdmin)
        {
            var query = _db.ApprovalRequests
                .AsNoTracking()
                .Include(a => a.RequestedByUser)
                .Where(a => a.ActionType == ApprovalActionType.DuplicateTask && a.Status == ApprovalStatus.Pending);

            if (!callerIsAdmin)
            {
                query = query.Where(a => _db.Tasks.Any(t => t.Id == a.TargetEntityId
                    && _db.ProjectMembers.Any(pm => pm.ProjectId == t.ProjectId && pm.UserId == callerId && pm.Role == UserRole.Manager)));
            }

            return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        }

        // Every request of one action type the caller has filed, any status —
        // the "Sent by me" side of an edit-request queue.
        public async Task<List<ApprovalRequest>> GetSentByAsync(int requestedByUserId, ApprovalActionType actionType)
        {
            return await _db.ApprovalRequests
                .AsNoTracking()
                .Include(a => a.RequestedByUser)
                .Where(a => a.ActionType == actionType && a.RequestedByUserId == requestedByUserId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        // AssignBusyUser requests: the payload names which user is being
        // asked about, and it isn't something SQL can filter on, so these
        // pull every pending one for the task/for the action type and match
        // the embedded UserId in memory — there are only ever a handful
        // pending at once.
        public async Task<bool> HasPendingBusyAssignmentAsync(int taskId, int userId)
        {
            var pending = await _db.ApprovalRequests
                .AsNoTracking()
                .Where(a => a.ActionType == ApprovalActionType.AssignBusyUser
                         && a.TargetEntityId == taskId
                         && a.Status == ApprovalStatus.Pending)
                .Select(a => a.PayloadJson)
                .ToListAsync();

            return pending.Any(json => ExtractUserId(json) == userId);
        }

        // The target user's own "Waiting on you" queue for busy-assignment
        // requests — separate from the Admin's, since nobody but that user
        // (or an Admin) resolves one of these.
        public async Task<List<ApprovalRequest>> GetPendingBusyAssignmentsForUserAsync(int userId)
        {
            var pending = await _db.ApprovalRequests
                .AsNoTracking()
                .Include(a => a.RequestedByUser)
                .Where(a => a.ActionType == ApprovalActionType.AssignBusyUser && a.Status == ApprovalStatus.Pending)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return pending.Where(a => ExtractUserId(a.PayloadJson) == userId).ToList();
        }

        private static int? ExtractUserId(string? payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(payloadJson);
                return doc.RootElement.TryGetProperty("UserId", out var prop) ? prop.GetInt32() : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}

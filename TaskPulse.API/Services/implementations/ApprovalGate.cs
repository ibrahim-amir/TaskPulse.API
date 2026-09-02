using System.Text.Json;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Repository.ApprovalRepo;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    public class ApprovalGate : IApprovalGate
    {
        private readonly ApprovalRequestRepository _approvalRepository;
        private readonly INotifier _notifier;
        private readonly IActivityLogger _activityLogger;

        public ApprovalGate(
            ApprovalRequestRepository approvalRepository,
            INotifier notifier,
            IActivityLogger activityLogger)
        {
            _approvalRepository = approvalRepository;
            _notifier = notifier;
            _activityLogger = activityLogger;
        }

        public async Task<ApprovalGateResult> ExecuteOrRequestApprovalAsync(
            ApprovalActionType actionType,
            string targetEntityType,
            int targetEntityId,
            bool needsApproval,
            int requestedByUserId,
            Func<Task> execute,
            object? payload = null)
        {
            if (!needsApproval)
            {
                await execute();
                return new ApprovalGateResult { Executed = true };
            }

            // One pending request per target at a time, so a Manager clicking
            // twice doesn't leave the Admin with a queue of duplicates.
            var existing = await _approvalRepository.GetPendingForTargetAsync(targetEntityType, targetEntityId);
            if (existing is not null)
            {
                return new ApprovalGateResult
                {
                    Executed = false,
                    AlreadyPending = true,
                    ApprovalRequestId = existing.Id
                };
            }

            var request = new ApprovalRequest
            {
                ActionType = actionType,
                TargetEntityType = targetEntityType,
                TargetEntityId = targetEntityId,
                RequestedByUserId = requestedByUserId,
                PayloadJson = payload is null ? null : JsonSerializer.Serialize(payload),
                Status = ApprovalStatus.Pending
            };

            await _approvalRepository.CreateAsync(request);

            await _notifier.NotifyAdminsAsync(
                NotificationType.ApprovalRequired,
                $"{actionType} on {targetEntityType} #{targetEntityId} is waiting for your approval.",
                nameof(ApprovalRequest),
                request.Id);

            await _activityLogger.LogAsync(
                requestedByUserId,
                ActivityAction.ApprovalRequested,
                $"{actionType} on {targetEntityType} #{targetEntityId}.");

            return new ApprovalGateResult { Executed = false, ApprovalRequestId = request.Id };
        }
    }
}

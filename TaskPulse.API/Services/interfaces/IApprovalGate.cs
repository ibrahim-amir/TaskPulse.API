using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Services.interfaces
{
    // The one place that decides "do this now" versus "ask the Admin first".
    //
    // A service calls it instead of performing a protected action directly:
    //
    //     var gate = await _approvalGate.ExecuteOrRequestApprovalAsync(
    //         ApprovalActionType.DeleteProject,
    //         nameof(Project), project.Id,
    //         needsApproval: !caller.IsAdmin && project.CreatedByRole == UserRole.Admin,
    //         requestedByUserId: callerId,
    //         execute: () => _projectRepository.DeleteAsync(project));
    //
    //     if (!gate.Executed) return "pending approval" to the caller;
    //
    // When approval isn't needed the action runs immediately. When it is, the
    // action does NOT run — a request is filed and the Admin is notified.
    public interface IApprovalGate
    {
        Task<ApprovalGateResult> ExecuteOrRequestApprovalAsync(
            ApprovalActionType actionType,
            string targetEntityType,
            int targetEntityId,
            bool needsApproval,
            int requestedByUserId,
            Func<Task> execute,
            object? payload = null);
    }

    public class ApprovalGateResult
    {
        // False means nothing happened yet and an approval is now pending.
        public bool Executed { get; set; }
        public int? ApprovalRequestId { get; set; }

        // Set when a request for this same target was already waiting, so the
        // caller can say so instead of filing a duplicate.
        public bool AlreadyPending { get; set; }
    }
}

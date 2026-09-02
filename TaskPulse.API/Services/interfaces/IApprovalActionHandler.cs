using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Services.interfaces
{
    // Carries out one kind of approved action. ApprovalService picks the
    // handler whose ActionType matches the request being approved.
    //
    // Adding a new gated action means: a new ApprovalActionType value, a new
    // handler class, and one registration line. Nothing existing changes.
    public interface IApprovalActionHandler
    {
        ApprovalActionType ActionType { get; }

        Task ExecuteAsync(ApprovalRequest request);
    }
}

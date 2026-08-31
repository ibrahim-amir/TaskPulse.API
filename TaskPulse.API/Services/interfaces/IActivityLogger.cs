using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Services.interfaces
{
    // Writes the audit trail. Call it from a service after the thing it
    // describes has actually been saved:
    //
    //     await _userRepository.UpdateUserAsync(user);
    //     await _activityLogger.LogAsync(user.Id, ActivityAction.UserRoleChanged, $"Role set to {user.Role}.");
    //
    // Login rows written through here are what the attendance endpoint reads.
    public interface IActivityLogger
    {
        Task LogAsync(int userId, ActivityAction action, string? details = null, int? projectId = null, int? taskId = null);
    }
}

using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Services.interfaces
{
    // Writes notifications. Same shape as IActivityLogger — call it from a
    // service after the thing it's telling someone about has been saved.
    //
    // RelatedEntityType/Id are what the frontend opens when the notification is
    // clicked, so an ApprovalRequired can take the Admin straight to the
    // request they need to answer.
    public interface INotifier
    {
        Task NotifyAsync(int userId, NotificationType type, string message, string? relatedEntityType = null, int? relatedEntityId = null);

        Task NotifyAdminsAsync(NotificationType type, string message, string? relatedEntityType = null, int? relatedEntityId = null);
    }
}

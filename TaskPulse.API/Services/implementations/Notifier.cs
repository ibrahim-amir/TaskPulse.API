using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Repository.NotificationRepo;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    public class Notifier : INotifier
    {
        private readonly NotificationRepository _notificationRepository;

        public Notifier(NotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Notification> NotifyAsync(int userId, NotificationType type, string message, string? relatedEntityType = null, int? relatedEntityId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Message = message,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId
            };

            // CreateAsync adds and saves in place, so the same instance has its
            // generated Id/CreatedAt populated once this completes.
            await _notificationRepository.CreateAsync(notification);
            return notification;
        }

        public async Task NotifyAdminsAsync(NotificationType type, string message, string? relatedEntityType = null, int? relatedEntityId = null)
        {
            var adminIds = await _notificationRepository.GetActiveAdminIdsAsync();

            foreach (var adminId in adminIds)
                await NotifyAsync(adminId, type, message, relatedEntityType, relatedEntityId);
        }
    }
}

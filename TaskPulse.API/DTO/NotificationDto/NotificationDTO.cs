using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.NotificationDto;

public class NotificationDTO
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public bool IsOutgoing { get; set; }
    public DateTime CreatedAt { get; set; }

    public static NotificationDTO FromEntity(Notification notification)
    {
        return new NotificationDTO
        {
            Id = notification.Id,
            Type = notification.Type,
            Message = notification.Message,
            ImageUrl = notification.ImageUrl,
            RelatedEntityType = notification.RelatedEntityType,
            RelatedEntityId = notification.RelatedEntityId,
            IsRead = notification.IsRead,
            IsOutgoing = notification.IsOutgoing,
            CreatedAt = notification.CreatedAt
        };
    }
}

using TaskPulse.API.Entities.BaseEntityClass;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Entities
{
    public class Notification : BasicEntityClass
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty;

        // Only ever set on a DirectMessage — an image attached to a chat message.
        public string? ImageUrl { get; set; }

        // What the notification is about, so the frontend can open it 
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }

        public bool IsRead { get; set; } = false;

        // True only on the sender's own mirror copy of a DirectMessage (see
        // NotificationService.SendAsync) — lets a conversation thread tell the
        // two sides of the same exchange apart without a separate Message table.
        public bool IsOutgoing { get; set; } = false;
    }
}

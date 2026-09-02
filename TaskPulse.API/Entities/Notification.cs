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

        // What the notification is about, so the frontend can open it 
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }

        public bool IsRead { get; set; } = false;
    }
}

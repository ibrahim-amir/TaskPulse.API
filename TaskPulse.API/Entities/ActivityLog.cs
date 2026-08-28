using TaskPulse.API.Entities.BaseEntityClass;

namespace TaskPulse.API.Entities
{
    public class ActivityLog : BasicEntityClass
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }
        public int? TaskId { get; set; }
        public TaskItem? Task { get; set; }
    }
}

using TaskPulse.API.Entities.BaseEntityClass;

namespace TaskPulse.API.Entities
{
    public class Comment : BasicEntityClass
    {

        public int TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string Body { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }
}

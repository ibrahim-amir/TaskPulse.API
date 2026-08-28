using TaskPulse.API.Entities.BaseEntityClass;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Entities
{
    public class TaskItem : BasicEntityClass
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;
        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

        public DateTime? DueDate { get; set; }
        public decimal? EstimatedHours { get; set; }
        public decimal? ActualHours { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public int? AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }

        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
    }
}

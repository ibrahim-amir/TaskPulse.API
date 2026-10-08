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

        // Set when a Manager/Admin closes the task after reviewing the uploads.
        public DateTime? CompletedDate { get; set; }

        // Stop the deadline monitor from telling the same people the same thing
        // on every pass.
        public bool DueSoonNotified { get; set; } = false;
        public bool OverdueNotified { get; set; } = false;

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public ICollection<TaskAssignment> Assignments { get; set; } = new List<TaskAssignment>();

        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();

        // The required file checklist. Progress is uploaded / total across these.
        public ICollection<TaskSubtask> Subtasks { get; set; } = new List<TaskSubtask>();

        // A separate, self-ticked checklist — no files involved.
        public ICollection<TaskTodoItem> TodoItems { get; set; } = new List<TaskTodoItem>();
    }
}

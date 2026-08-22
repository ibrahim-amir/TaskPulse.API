namespace TaskPulse.API.Entities
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;
        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int ManagerId { get; set; }
        public User Manager { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}

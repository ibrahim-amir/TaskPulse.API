using TaskPulse.API.Entities.BaseEntityClass;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Entities
{
    public class TaskAssignment : BasicEntityClass
    {
        public int TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Who assigned this if Admin, a Manager can't remove it without Admin approval.
        public UserRole AssignedByRole { get; set; }
    }
}

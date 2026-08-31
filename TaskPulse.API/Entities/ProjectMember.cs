using TaskPulse.API.Entities.BaseEntityClass;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Entities
{
    public class ProjectMember : BasicEntityClass
    {
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public UserRole Role { get; set; }
    }
}

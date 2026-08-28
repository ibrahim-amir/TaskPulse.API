using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities.BaseEntityClass;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.Entities
{
    public class User : BasicEntityClass
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Member;
        public string? profileImageUrl { get; set; }
        public bool IsDisabled { get; set; } = false;


        public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
        public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
        public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<TaskAttachment> UploadedAttachments { get; set; } = new List<TaskAttachment>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
        public static User CreateUserFromDto(CreateUserDto dto)
        {
            return new User
            {
                Name = dto.Name,
                Email = dto.Email,
                profileImageUrl = dto.profileImageUrl,
                Role = dto.Role
            };
        }
    }
}

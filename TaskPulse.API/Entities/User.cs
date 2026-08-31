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
        public string? profileImageUrl { get; set; }
        public bool IsDisabled { get; set; } = false;
        public UserRole Role { get; set; } = UserRole.Member;

        public bool IsEmailConfirmed { get; set; } = false;
        public string? EmailConfirmationTokenHash { get; set; }
        public DateTime? EmailConfirmationTokenExpiresAt { get; set; }

        public string? PasswordResetTokenHash { get; set; }
        public DateTime? PasswordResetTokenExpiresAt { get; set; }

        // Stored hashed, same as PasswordHash.
        public string? RefreshTokenHash { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public bool RefreshTokenRememberMe { get; set; } = false;

        public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
        public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
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

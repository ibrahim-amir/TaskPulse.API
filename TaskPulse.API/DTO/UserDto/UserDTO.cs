using TaskPulse.API.DTO.UserDto.BaseUserClass;
using TaskPulse.API.Entities;

namespace TaskPulse.API.DTO.UserDto
{
    public class UserDTO : BasicUserDto
    {
        public int Id { get; set; }
        public string? Role { get; set; }
        public string? profileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDisabled { get; set; }

        // Maps a saved User entity onto the shape that leaves the API.
        // This never touches PasswordHash — it's the only path a User's data
        // takes on its way out, so leaving the hash out here is what actually
        // keeps it from ever reaching a response.
        public static UserDTO FromEntity(User user) => new UserDTO
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            profileImageUrl = user.profileImageUrl,
            CreatedAt = user.CreatedAt,
            IsDisabled = user.IsDisabled
        };
    }
}

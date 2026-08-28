namespace TaskPulse.API.DTO.UserDto.BaseUserClass
{
    public class BasicUserDto
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

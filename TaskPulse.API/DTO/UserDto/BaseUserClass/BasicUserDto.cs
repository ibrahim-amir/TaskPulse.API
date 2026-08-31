namespace TaskPulse.API.DTO.UserDto.BaseUserClass
{
    // Fields shared by input and output user DTOs. Password is input-only,
    // so it lives on CreateUserDto, not here.
    public class BasicUserDto
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = string.Empty;
    }
}

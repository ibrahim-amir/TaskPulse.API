namespace TaskPulse.API.DTO.AttendanceDto;

// Query string for GET /api/users/{id}/attendance.
// Both optional — leaving them out returns the current calendar month.
public class AttendanceQueryDto
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

namespace TaskPulse.API.DTO.AttendanceDto;

// One calendar day. Present means the user logged in at least once that day —
// access tokens last 24 hours, so a login is a day of attendance.
public class AttendanceDayDto
{
    public DateOnly Date { get; set; }
    public bool IsPresent { get; set; }
    public DateTime? FirstLoginAt { get; set; }
}

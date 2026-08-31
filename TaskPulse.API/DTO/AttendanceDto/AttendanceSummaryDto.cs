namespace TaskPulse.API.DTO.AttendanceDto;

// Response for GET /api/users/{id}/attendance. Days covers every date in the
// range, present or not, so the frontend can draw the calendar straight from it.
public class AttendanceSummaryDto
{
    public int UserId { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int TotalDays { get; set; }
    public int DaysPresent { get; set; }
    public List<AttendanceDayDto> Days { get; set; } = new();
}

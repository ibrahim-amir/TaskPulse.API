namespace TaskPulse.API.DTO.DashboardDto;

// One row of the project report's performance table.
public class UserPerformanceDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int AssignedTaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public int OverdueTaskCount { get; set; }
    public int CompletionPercent { get; set; }
    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }
}

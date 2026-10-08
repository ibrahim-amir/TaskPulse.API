namespace TaskPulse.API.DTO.DashboardDto;

// GET /api/dashboard/summary. TasksByStatus and TasksByPriority are the two
// dashboard charts; the counters above them are the header tiles.
public class DashboardSummaryDto
{
    public int ProjectCount { get; set; }
    public int ActiveProjectCount { get; set; }
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public int OverdueTaskCount { get; set; }
    public int MyOpenTaskCount { get; set; }
    public int UnreadNotificationCount { get; set; }
    public int PendingApprovalCount { get; set; }

    public List<ChartSliceDto> TasksByStatus { get; set; } = new();
    public List<ChartSliceDto> TasksByPriority { get; set; } = new();
}

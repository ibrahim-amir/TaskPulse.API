namespace TaskPulse.API.DTO.DashboardDto;

// GET /api/dashboard/project/{projectId}. The per-project report: completion,
// the same two charts scoped to this project, and who has done what.
public class ProjectDashboardDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public int OverdueTaskCount { get; set; }
    public int CompletionPercent { get; set; }

    // Across the whole project's required-file checklists, so a project that's
    // "80% of files in" reads that way even before tasks close.
    public int RequiredFileCount { get; set; }
    public int UploadedFileCount { get; set; }
    public int FileProgressPercent { get; set; }

    public List<ChartSliceDto> TasksByStatus { get; set; } = new();
    public List<ChartSliceDto> TasksByPriority { get; set; } = new();
    public List<UserPerformanceDto> UserPerformance { get; set; } = new();
}

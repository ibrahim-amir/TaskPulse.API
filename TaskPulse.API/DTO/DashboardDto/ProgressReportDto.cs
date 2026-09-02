namespace TaskPulse.API.DTO.DashboardDto;

// GET /api/dashboard/progress. Recommended-versus-actual time, and how the
// open work sits against its deadlines.
public class ProgressReportDto
{
    public decimal TotalEstimatedHours { get; set; }
    public decimal TotalActualHours { get; set; }

    // Above 100 means the work is taking longer than estimated.
    public int EffortVariancePercent { get; set; }

    public DeadlineHealthDto DeadlineHealth { get; set; } = new();
    public List<ProjectProgressDto> Projects { get; set; } = new();
}

// Open tasks bucketed by how their due date is looking. Late is past due;
// AtRisk is due within the next three days; the rest are OnTrack. Tasks with
// no due date can't be judged, so they're counted separately.
public class DeadlineHealthDto
{
    public int OnTrack { get; set; }
    public int AtRisk { get; set; }
    public int Late { get; set; }
    public int NoDueDate { get; set; }
}

public class ProjectProgressDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public int CompletionPercent { get; set; }
    public int OverdueTaskCount { get; set; }
    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }
}

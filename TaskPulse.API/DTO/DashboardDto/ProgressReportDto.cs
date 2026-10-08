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

    // Every (priority, status) combination across the same scope as the rest
    // of this report, including zero-count cells — a heatmap grid, not just
    // a list of what happens to exist.
    public List<PriorityStatusCellDto> PriorityStatusMatrix { get; set; } = new();
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

    // This project's own slice of the same On track/At risk/Late/No due
    // date buckets as the org-wide DeadlineHealthDto above.
    public DeadlineHealthDto DeadlineHealth { get; set; } = new();
}

// One cell of the priority x status heatmap.
public class PriorityStatusCellDto
{
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

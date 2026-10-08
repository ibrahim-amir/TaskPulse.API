namespace TaskPulse.API.DTO.AiDto;

// One row of the AI risk report's team breakdown — who's behind, who's on
// track, who has room for more work. Computed deterministically from task
// data (not by the model), so it's always present and never hallucinated.
public class TeamMemberAssessmentDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    // "NeedsAttention" (has overdue work), "HasCapacity" (nothing open right
    // now), or "OnTrack" (working open tasks, none overdue).
    public string Status { get; set; } = string.Empty;

    public int AssignedTaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public int OpenTaskCount { get; set; }
    public int OverdueTaskCount { get; set; }
    public int CompletionPercent { get; set; }
    public string Note { get; set; } = string.Empty;
}

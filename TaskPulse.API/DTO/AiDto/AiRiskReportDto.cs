namespace TaskPulse.API.DTO.AiDto;

// GET /api/projects/{id}/ai-risks — a per-project risk scan: an overall
// status, a "what could go wrong" list with suggested actions (both AI
// generated), and a deterministic per-member workload breakdown. Generated
// fresh on each request, nothing is cached or stored.
public class AiRiskReportDto
{
    public int ProjectId { get; set; }

    // "OnTrack", "AtRisk" or "Late" — same vocabulary as the deadline-health
    // report, computed the same way (by due dates), not by the model.
    public string ProjectStatus { get; set; } = string.Empty;
    public string ProjectStatusReason { get; set; } = string.Empty;

    public List<string> Risks { get; set; } = new();
    public List<string> SuggestedActions { get; set; } = new();
    public List<TeamMemberAssessmentDto> TeamAssessments { get; set; } = new();

    public DateTime GeneratedAt { get; set; }
}

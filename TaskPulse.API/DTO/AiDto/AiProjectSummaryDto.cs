namespace TaskPulse.API.DTO.AiDto;

// GET /api/projects/{id}/ai-summary — a natural-language status readout,
// generated fresh on each request rather than stored.
public class AiProjectSummaryDto
{
    public int ProjectId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}

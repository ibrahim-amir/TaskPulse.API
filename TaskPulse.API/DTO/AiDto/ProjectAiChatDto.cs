namespace TaskPulse.API.DTO.AiDto;

// POST /api/projects/{id}/ai-insights/chat. History is the on-screen
// conversation so far (oldest first) — nothing is persisted server-side,
// the frontend just echoes it back each turn.
public class ProjectAiChatRequestDto
{
    public string Question { get; set; } = string.Empty;
    public List<AiChatTurnDto> History { get; set; } = new();
}

public class ProjectAiChatResponseDto
{
    public string Answer { get; set; } = string.Empty;
}

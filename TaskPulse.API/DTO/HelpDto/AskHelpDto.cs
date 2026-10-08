namespace TaskPulse.API.DTO.HelpDto;

// POST /api/help/ask. PageContext is the frontend route the user was on
// (e.g. "/projects/12/board") when they asked — lets the assistant tailor
// its answer to what's actually on screen instead of guessing. History is
// the on-screen conversation so far (oldest first) — nothing is persisted
// server-side, the frontend just echoes it back each turn.
public class AskHelpDto
{
    public string Question { get; set; } = string.Empty;
    public string? PageContext { get; set; }
    public List<HelpChatTurnDto> History { get; set; } = new();
}

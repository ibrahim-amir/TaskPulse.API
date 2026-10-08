namespace TaskPulse.API.DTO.AiDto;

// One prior message in the on-screen conversation, echoed back by the
// frontend so the assistant has context for follow-up questions. Role is
// "user" or "model" — Gemini's own turn-role vocabulary.
public class AiChatTurnDto
{
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

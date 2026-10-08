namespace TaskPulse.API.DTO.AiDto;

// One AI-proposed task — a preview, not yet a real TaskItem. The frontend
// shows these for review; only the ones the user keeps get POSTed to
// /api/projects/{id}/ai-insights/tasks to actually create them.
public class AiTaskSuggestionDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }

    // Low | Medium | High | Critical — matches PriorityLevel's names.
    public string Priority { get; set; } = "Medium";
    public decimal? EstimatedHours { get; set; }
}

// POST /api/projects/{id}/ai-insights/task-suggestions. Description is
// optional free text ("what do you want done") — left blank, suggestions
// are derived from the project's current overdue/high-priority work instead.
public class GenerateTaskSuggestionsDto
{
    public string? Description { get; set; }
}

public class AiTaskSuggestionsResultDto
{
    public List<AiTaskSuggestionDto> Tasks { get; set; } = new();
}

// POST /api/projects/{id}/ai-insights/tasks — the suggestions the user kept,
// sent back to actually create them as real tasks.
public class AddAiTasksDto
{
    public List<AiTaskSuggestionDto> Tasks { get; set; } = new();
}

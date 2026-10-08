using TaskPulse.API.DTO.TaskDto;

namespace TaskPulse.API.DTO.AiDto;

public class AddAiTasksResultDto
{
    public List<TaskDTO> Created { get; set; } = new();

    // "<title>: <reason>" for any suggestion that didn't validate as a real
    // task (e.g. a title Gemini let run past the real 200-char limit).
    public List<string> Failed { get; set; } = new();
}

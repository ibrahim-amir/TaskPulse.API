using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.AiDto;

namespace TaskPulse.API.Services.interfaces
{
    public interface IAiInsightsService
    {
        Task<CommonResponse<AiProjectSummaryDto>> GetProjectSummaryAsync(int projectId, int callerId, bool callerIsAdmin);

        Task<CommonResponse<AiRiskReportDto>> GetProjectRisksAsync(int projectId, int callerId, bool callerIsAdmin);

        // A project-scoped chat — grounded in the same live facts as the risk
        // report, so questions like "what should I do next?" get answers
        // specific to this project rather than the site-navigation help bot.
        Task<CommonResponse<ProjectAiChatResponseDto>> AskProjectInsightAsync(int projectId, int callerId, bool callerIsAdmin, ProjectAiChatRequestDto dto);

        // Proposes tasks (title/description/due date/priority/estimate) —
        // either from free text, or, left blank, from the project's current
        // overdue/high-priority signals. Nothing is created yet.
        Task<CommonResponse<AiTaskSuggestionsResultDto>> GenerateTaskSuggestionsAsync(int projectId, int callerId, bool callerIsAdmin, GenerateTaskSuggestionsDto dto);

        // Turns the suggestions the user kept into real tasks via the normal
        // task-creation path (so the usual Manager/Admin-only rule applies).
        Task<CommonResponse<AddAiTasksResultDto>> AddGeneratedTasksAsync(int projectId, int callerId, bool callerIsAdmin, AddAiTasksDto dto);
    }
}

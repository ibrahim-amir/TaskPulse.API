using System.Net;
using System.Text.Json;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.AiDto;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Repository.DashboardRepo;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Repository.TaskRepo;
using TaskPulse.API.Services.Infrastructure;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    // Turns a project's live task data into AI features:
    // a plain-language status
    // summary, a risk scan (overdue/high-priority/deadline/workload signals
    // -> Gemini -> "Potential Risks" + "Suggested Actions" + a deterministic
    // per-member workload table), a project-scoped chat, and AI-proposed
    // tasks the user can review and add. Nothing here is cached or stored —
    // every call is generated fresh.
    public class AiInsightsService : BaseServiceClass, IAiInsightsService
    {
        // Same cutoff the org-wide deadline-health report uses, so "at risk"
        // means the same thing everywhere in the app.
        private const int AtRiskWithinDays = 3;
        private const int MaxChatHistoryTurns = 12;

        private readonly ProjectRepository _projectRepository;
        private readonly TaskRepository _taskRepository;
        private readonly DashboardRepository _dashboardRepository;
        private readonly ITaskService _taskService;
        private readonly GeminiClient _geminiClient;

        public AiInsightsService(
            CustomExceptionHandler customExceptionHandler,
            ProjectRepository projectRepository,
            TaskRepository taskRepository,
            DashboardRepository dashboardRepository,
            ITaskService taskService,
            GeminiClient geminiClient)
            : base(customExceptionHandler)
        {
            _projectRepository = projectRepository;
            _taskRepository = taskRepository;
            _dashboardRepository = dashboardRepository;
            _taskService = taskService;
            _geminiClient = geminiClient;
        }

        public async Task<CommonResponse<AiProjectSummaryDto>> GetProjectSummaryAsync(int projectId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<AiProjectSummaryDto>();

            try
            {
                var (project, tasks, denied) = await LoadProjectAsync<AiProjectSummaryDto>(projectId, callerId, callerIsAdmin);
                if (denied is not null)
                    return denied;

                var facts = BuildFacts(project!, tasks!);

                var prompt = $$"""
                    You are a project status reporter for a task-tracking app called TaskPulse.
                    Write a short, plain-language status summary of the project below for a manager glancing at a dashboard.

                    Project: {{facts.ProjectName}}
                    Completion: {{facts.CompletionPercent}}% ({{facts.CompletedCount}} of {{facts.TaskCount}} tasks done)
                    Overdue tasks: {{facts.Overdue.Count}}
                    High-priority tasks still open: {{facts.HighPriorityOpen.Count}}
                    Tasks due in the next 7 days: {{facts.UpcomingDeadlines.Count}}

                    Write 2-4 sentences, in the style of: "Project Status: 68% complete. The team has completed 17 of 25 tasks. Most development tasks are finished, but 4 high-priority tasks remain. Two tasks are overdue and should be addressed before the project deadline."
                    Only state facts given above — do not invent task names, people, or numbers not provided. Plain text only, no markdown, no bullet points.
                    """;

                var text = await _geminiClient.GenerateAsync(prompt);
                if (text is null)
                    return CommonResponseFactory.CreateErrorResponse<AiProjectSummaryDto>(
                        "The AI summary is temporarily unavailable. Please try again shortly.", HttpStatusCode.ServiceUnavailable);

                var summary = new AiProjectSummaryDto
                {
                    ProjectId = project!.Id,
                    Summary = text.Trim(),
                    GeneratedAt = DateTime.UtcNow
                };

                return CommonResponseFactory.CreateSuccessResponse(summary, "AI summary generated.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<AiRiskReportDto>> GetProjectRisksAsync(int projectId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<AiRiskReportDto>();

            try
            {
                var (project, tasks, denied) = await LoadProjectAsync<AiRiskReportDto>(projectId, callerId, callerIsAdmin);
                if (denied is not null)
                    return denied;

                var facts = BuildFacts(project!, tasks!);
                var (status, statusReason) = ClassifyProjectStatus(facts);
                var team = await BuildTeamAsync(projectId);

                var prompt = BuildRiskPrompt(facts, status, statusReason, team);

                var text = await _geminiClient.GenerateAsync(prompt);
                if (text is null)
                    return CommonResponseFactory.CreateErrorResponse<AiRiskReportDto>(
                        "AI risk detection is temporarily unavailable. Please try again shortly.", HttpStatusCode.ServiceUnavailable);

                var report = ParseRiskReport(text, project!.Id);
                if (report is null)
                    return CommonResponseFactory.CreateErrorResponse<AiRiskReportDto>(
                        "The AI risk report came back in an unexpected format. Please try again.", HttpStatusCode.BadGateway);

                report.ProjectStatus = status;
                report.ProjectStatusReason = statusReason;
                report.TeamAssessments = team;

                return CommonResponseFactory.CreateSuccessResponse(report, "AI risk report generated.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        // A chat grounded in the same live facts as the risk report, so
        // follow-up questions ("what should I do next?") get answers
        // specific to this project.
        public async Task<CommonResponse<ProjectAiChatResponseDto>> AskProjectInsightAsync(int projectId, int callerId, bool callerIsAdmin, ProjectAiChatRequestDto dto)
        {
            var response = new CommonResponse<ProjectAiChatResponseDto>();

            try
            {
                var question = dto.Question?.Trim() ?? string.Empty;
                if (question.Length == 0)
                    return CommonResponseFactory.CreateErrorResponse<ProjectAiChatResponseDto>("Ask a question first.", HttpStatusCode.BadRequest);
                if (question.Length > 500)
                    return CommonResponseFactory.CreateErrorResponse<ProjectAiChatResponseDto>("Keep the question to 500 characters or fewer.", HttpStatusCode.BadRequest);

                var (project, tasks, denied) = await LoadProjectAsync<ProjectAiChatResponseDto>(projectId, callerId, callerIsAdmin);
                if (denied is not null)
                    return denied;

                var facts = BuildFacts(project!, tasks!);
                var (status, statusReason) = ClassifyProjectStatus(facts);
                var team = await BuildTeamAsync(projectId);

                var systemInstruction = $$"""
                    You are an AI project advisor inside TaskPulse, helping with the project "{{facts.ProjectName}}". Answer questions about this project's status, risks and what to do next — using ONLY the data below. Never invent tasks, people, or numbers not given. The conversation may have earlier turns; use them for context. Keep answers short and practical (1-4 sentences, plain text, no markdown).

                    Overall status: {{status}} ({{statusReason}})
                    Completion: {{facts.CompletionPercent}}% ({{facts.CompletedCount}} of {{facts.TaskCount}} tasks done)

                    Overdue tasks ({{facts.Overdue.Count}}):
                    {{FormatTaskLines(facts.Overdue)}}

                    High-priority tasks still open ({{facts.HighPriorityOpen.Count}}):
                    {{FormatTaskLines(facts.HighPriorityOpen)}}

                    Tasks due in the next 7 days ({{facts.UpcomingDeadlines.Count}}):
                    {{FormatTaskLines(facts.UpcomingDeadlines)}}

                    Team (name: open tasks / completion %, overdue count, status):
                    {{FormatTeamLines(team)}}

                    If the user wants tasks created or a plan turned into work items, describe what you'd propose, and mention they can use this panel's "Generate tasks" tool to actually add them to the project — you cannot create tasks yourself.
                    """;

                var turns = (dto.History ?? new List<AiChatTurnDto>())
                    .Where(t => !string.IsNullOrWhiteSpace(t.Text))
                    .TakeLast(MaxChatHistoryTurns)
                    .Select(t => (Role: t.Role == "model" ? "model" : "user", Text: t.Text.Length > 2000 ? t.Text[..2000] : t.Text))
                    .Append((Role: "user", Text: question))
                    .ToList();

                var text = await _geminiClient.GenerateChatAsync(systemInstruction, turns);
                if (text is null)
                    return CommonResponseFactory.CreateErrorResponse<ProjectAiChatResponseDto>(
                        "The project assistant is temporarily unavailable. Please try again shortly.", HttpStatusCode.ServiceUnavailable);

                return CommonResponseFactory.CreateSuccessResponse(new ProjectAiChatResponseDto { Answer = text.Trim() }, "Answered.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        // Proposes tasks — from free text if given, otherwise derived from
        // the project's own overdue/high-priority signals. Nothing is
        // created yet; the frontend shows these for review first.
        public async Task<CommonResponse<AiTaskSuggestionsResultDto>> GenerateTaskSuggestionsAsync(int projectId, int callerId, bool callerIsAdmin, GenerateTaskSuggestionsDto dto)
        {
            var response = new CommonResponse<AiTaskSuggestionsResultDto>();

            try
            {
                if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Length > 1000)
                    return CommonResponseFactory.CreateErrorResponse<AiTaskSuggestionsResultDto>(
                        "Keep the description to 1000 characters or fewer.", HttpStatusCode.BadRequest);

                var (project, tasks, denied) = await LoadProjectAsync<AiTaskSuggestionsResultDto>(projectId, callerId, callerIsAdmin);
                if (denied is not null)
                    return denied;

                var facts = BuildFacts(project!, tasks!);
                var basis = BuildTaskGenerationBasis(facts, dto.Description);

                var existingTitles = tasks!
                    .Where(t => t.Status != TaskItemStatus.Done)
                    .Select(t => $"- \"{t.Title}\"")
                    .ToList();

                var prompt = $$"""
                    You are a project planning assistant for a task-tracking app called TaskPulse. Based on the request below, propose a short list of concrete, actionable tasks to add to the project "{{facts.ProjectName}}".

                    Request:
                    {{basis}}

                    Tasks already in the project (do not propose duplicates of these):
                    {{(existingTitles.Count == 0 ? "- none" : string.Join("\n", existingTitles))}}

                    Respond with ONLY valid JSON, no markdown fences, no commentary, in exactly this shape:
                    {"tasks": [{"title": "...", "description": "...", "dueInDays": 7, "priority": "Medium", "estimatedHours": 4}, ...]}

                    Rules:
                    - Propose between 1 and 8 tasks.
                    - "title" must be under 100 characters, specific and actionable.
                    - "description" is one short sentence of context (may be an empty string).
                    - "dueInDays" is a whole number of days from today (1-30) — your best estimate of a reasonable deadline.
                    - "priority" must be exactly one of: Low, Medium, High, Critical.
                    - "estimatedHours" is a number between 1 and 40 (your best estimate), or null if unsure.
                    - Do not duplicate any task already listed above.
                    """;

                var text = await _geminiClient.GenerateAsync(prompt);
                if (text is null)
                    return CommonResponseFactory.CreateErrorResponse<AiTaskSuggestionsResultDto>(
                        "Task generation is temporarily unavailable. Please try again shortly.", HttpStatusCode.ServiceUnavailable);

                var suggestions = ParseTaskSuggestions(text);
                if (suggestions is null)
                    return CommonResponseFactory.CreateErrorResponse<AiTaskSuggestionsResultDto>(
                        "The AI response came back in an unexpected format. Please try again.", HttpStatusCode.BadGateway);

                return CommonResponseFactory.CreateSuccessResponse(new AiTaskSuggestionsResultDto { Tasks = suggestions }, "Task suggestions generated.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        // Turns the kept suggestions into real tasks via the normal
        // creation path, so the usual Manager/Admin-only rule applies and
        // nothing here bypasses validation.
        public async Task<CommonResponse<AddAiTasksResultDto>> AddGeneratedTasksAsync(int projectId, int callerId, bool callerIsAdmin, AddAiTasksDto dto)
        {
            var response = new CommonResponse<AddAiTasksResultDto>();

            try
            {
                var project = await _projectRepository.GetByIdAsync(projectId);
                if (project is null)
                    return CommonResponseFactory.CreateErrorResponse<AddAiTasksResultDto>("Project not found.", HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckManager(project, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<AddAiTasksResultDto>(access, HttpStatusCode.Forbidden);

                if (dto.Tasks.Count == 0)
                    return CommonResponseFactory.CreateErrorResponse<AddAiTasksResultDto>("No tasks to add.", HttpStatusCode.BadRequest);

                var result = new AddAiTasksResultDto();

                foreach (var suggestion in dto.Tasks)
                {
                    var title = suggestion.Title?.Trim() ?? string.Empty;

                    var createDto = new CreateTaskDto
                    {
                        ProjectId = projectId,
                        Title = title,
                        Description = string.IsNullOrWhiteSpace(suggestion.Description) ? null : suggestion.Description.Trim(),
                        Priority = Enum.TryParse<PriorityLevel>(suggestion.Priority, ignoreCase: true, out var priority) ? priority : PriorityLevel.Medium,
                        DueDate = suggestion.DueDate,
                        EstimatedHours = suggestion.EstimatedHours
                    };

                    var created = await _taskService.CreateTaskAsync(createDto, callerId, callerIsAdmin);
                    if (created.Success && created.Data is not null)
                        result.Created.Add(created.Data);
                    else
                        result.Failed.Add($"{title}: {string.Join(" ", created.Errors)}");
                }

                var message = result.Failed.Count == 0
                    ? $"Added {result.Created.Count} task(s)."
                    : $"Added {result.Created.Count} task(s), {result.Failed.Count} failed.";

                return CommonResponseFactory.CreateSuccessResponse(result, message);
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        // Loads the project + its tasks and checks membership, returning a
        // ready-to-return error response for either "not found" or "not
        // allowed" so every read-only method shares one access-check path.
        private async Task<(Project? Project, List<TaskItem>? Tasks, CommonResponse<T>? Denied)> LoadProjectAsync<T>(int projectId, int callerId, bool callerIsAdmin)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project is null)
                return (null, null, CommonResponseFactory.CreateErrorResponse<T>("Project not found.", HttpStatusCode.NotFound));

            var access = ProjectAccess.CheckMember(project, callerId, callerIsAdmin);
            if (access is not null)
                return (null, null, CommonResponseFactory.CreateErrorResponse<T>(access, HttpStatusCode.Forbidden));

            var tasks = await _taskRepository.GetAllForProjectWithAssigneesAsync(projectId);
            return (project, tasks, null);
        }

        private async Task<List<TeamMemberAssessmentDto>> BuildTeamAsync(int projectId)
        {
            var totals = await _dashboardRepository.GetUserTotalsForProjectAsync(projectId);
            return totals.Select(BuildTeamAssessment).OrderBy(RankByAttention).ToList();
        }

        private record ProjectFacts(
            string ProjectName,
            int TaskCount,
            int CompletedCount,
            int CompletionPercent,
            List<TaskItem> Overdue,
            List<TaskItem> HighPriorityOpen,
            List<TaskItem> UpcomingDeadlines);

        private static ProjectFacts BuildFacts(Project project, List<TaskItem> tasks)
        {
            var now = DateTime.UtcNow;
            var completed = tasks.Count(t => t.Status == TaskItemStatus.Done);

            var overdue = tasks
                .Where(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != TaskItemStatus.Done)
                .ToList();

            var highPriorityOpen = tasks
                .Where(t => (t.Priority == PriorityLevel.High || t.Priority == PriorityLevel.Critical) && t.Status != TaskItemStatus.Done)
                .ToList();

            var upcoming = tasks
                .Where(t => t.DueDate.HasValue && t.DueDate.Value >= now && t.DueDate.Value <= now.AddDays(7) && t.Status != TaskItemStatus.Done)
                .ToList();

            return new ProjectFacts(
                project.Name,
                tasks.Count,
                completed,
                tasks.Count == 0 ? 0 : (int)Math.Round(completed * 100.0 / tasks.Count),
                overdue,
                highPriorityOpen,
                upcoming);
        }

        // Same rule the org-wide deadline-health report applies per task,
        // rolled up to one label for the whole project: any task already
        // late makes the project "Late"; nothing late but something due
        // inside the risk window makes it "AtRisk"; otherwise "OnTrack".
        private static (string Status, string Reason) ClassifyProjectStatus(ProjectFacts facts)
        {
            if (facts.Overdue.Count > 0)
                return ("Late", $"{facts.Overdue.Count} task(s) are past their due date.");

            var dueSoon = facts.UpcomingDeadlines.Count(t => t.DueDate!.Value <= DateTime.UtcNow.AddDays(AtRiskWithinDays));
            if (dueSoon > 0)
                return ("AtRisk", $"{dueSoon} task(s) are due within {AtRiskWithinDays} days.");

            return ("OnTrack", "Nothing overdue or due imminently.");
        }

        private static TeamMemberAssessmentDto BuildTeamAssessment(UserTaskTotals totals)
        {
            var openCount = totals.AssignedTaskCount - totals.CompletedTaskCount;
            var completionPercent = totals.AssignedTaskCount == 0
                ? 0
                : (int)Math.Round(totals.CompletedTaskCount * 100.0 / totals.AssignedTaskCount);

            string status;
            string note;

            if (totals.OverdueTaskCount > 0)
            {
                status = "NeedsAttention";
                note = $"{totals.OverdueTaskCount} of {totals.AssignedTaskCount} assigned task(s) are overdue.";
            }
            else if (openCount == 0)
            {
                status = "HasCapacity";
                note = totals.AssignedTaskCount == 0
                    ? "Not currently assigned to any tasks on this project."
                    : "All assigned tasks are complete — has room for more work.";
            }
            else
            {
                status = "OnTrack";
                note = $"{openCount} task(s) in progress, none overdue.";
            }

            return new TeamMemberAssessmentDto
            {
                UserId = totals.UserId,
                UserName = totals.UserName,
                Status = status,
                AssignedTaskCount = totals.AssignedTaskCount,
                CompletedTaskCount = totals.CompletedTaskCount,
                OpenTaskCount = openCount,
                OverdueTaskCount = totals.OverdueTaskCount,
                CompletionPercent = completionPercent,
                Note = note
            };
        }

        // NeedsAttention first (most actionable), then OnTrack, then
        // HasCapacity — so the people who most need a look sort to the top.
        private static int RankByAttention(TeamMemberAssessmentDto member) => member.Status switch
        {
            "NeedsAttention" => 0,
            "OnTrack" => 1,
            _ => 2
        };

        private static string BuildRiskPrompt(ProjectFacts facts, string status, string statusReason, List<TeamMemberAssessmentDto> team)
        {
            return $$"""
                You are a project risk analyst for a task-tracking app called TaskPulse.
                Analyze the project data below and identify concrete risks, then suggest actions — including reassignment where someone is overloaded/behind and someone else has room for more work.

                Project: {{facts.ProjectName}}
                Overall status: {{status}} ({{statusReason}})
                Completion: {{facts.CompletionPercent}}% ({{facts.CompletedCount}} of {{facts.TaskCount}} tasks done)

                Overdue tasks ({{facts.Overdue.Count}}):
                {{FormatTaskLines(facts.Overdue)}}

                High-priority tasks still open ({{facts.HighPriorityOpen.Count}}):
                {{FormatTaskLines(facts.HighPriorityOpen)}}

                Tasks due in the next 7 days ({{facts.UpcomingDeadlines.Count}}):
                {{FormatTaskLines(facts.UpcomingDeadlines)}}

                Team (name: open tasks / completion %, overdue count, status):
                {{FormatTeamLines(team)}}

                Respond with ONLY valid JSON, no markdown fences, no commentary, in exactly this shape:
                {"risks": ["short risk sentence", ...], "suggestedActions": ["short action sentence", ...]}

                Base every item strictly on the data above (task titles, people's names, counts given) — do not invent tasks, people, or numbers.
                If there is nothing concerning in a category, do not force a risk for it. If there are no risks at all, return {"risks": [], "suggestedActions": []}.
                Keep each item to one short sentence, in the style of: "Payment integration is overdue by 2 days." or "Reassign one of Sarah's tasks to John, who has capacity."
                """;
        }

        // No user text given for task generation — build the basis from the
        // project's own overdue/high-priority signals instead of making a
        // second AI round-trip just to re-derive what's already known.
        private static string BuildTaskGenerationBasis(ProjectFacts facts, string? userDescription)
        {
            if (!string.IsNullOrWhiteSpace(userDescription))
                return userDescription.Trim();

            var lines = new List<string>();

            if (facts.Overdue.Count > 0)
                lines.Add($"Address these overdue tasks: {string.Join(", ", facts.Overdue.Select(t => $"\"{t.Title}\""))}.");

            if (facts.HighPriorityOpen.Count > 0)
                lines.Add($"Make progress on these high-priority open tasks: {string.Join(", ", facts.HighPriorityOpen.Select(t => $"\"{t.Title}\""))}.");

            if (lines.Count == 0)
                lines.Add("Propose reasonable next tasks to keep this project moving toward completion.");

            return string.Join(" ", lines);
        }

        private static string FormatTaskLines(List<TaskItem> tasks)
        {
            if (tasks.Count == 0)
                return "- none";

            return string.Join("\n", tasks.Select(t =>
            {
                var due = t.DueDate.HasValue ? t.DueDate.Value.ToString("yyyy-MM-dd") : "no due date";
                return $"- \"{t.Title}\" (priority: {t.Priority}, status: {t.Status}, due: {due})";
            }));
        }

        private static string FormatTeamLines(List<TeamMemberAssessmentDto> team)
        {
            if (team.Count == 0)
                return "- no members";

            return string.Join("\n", team.Select(m =>
                $"- {m.UserName}: {m.OpenTaskCount} open / {m.CompletionPercent}% complete, {m.OverdueTaskCount} overdue, status: {m.Status}"));
        }

        private record GeminiRiskPayload(List<string>? Risks, List<string>? SuggestedActions);

        private static AiRiskReportDto? ParseRiskReport(string text, int projectId)
        {
            var json = StripCodeFence(text);

            try
            {
                var payload = JsonSerializer.Deserialize<GeminiRiskPayload>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (payload is null)
                    return null;

                return new AiRiskReportDto
                {
                    ProjectId = projectId,
                    Risks = payload.Risks ?? new List<string>(),
                    SuggestedActions = payload.SuggestedActions ?? new List<string>(),
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private record GeminiTaskSuggestion(string? Title, string? Description, int? DueInDays, string? Priority, decimal? EstimatedHours);
        private record GeminiTaskSuggestionsPayload(List<GeminiTaskSuggestion>? Tasks);

        private static List<AiTaskSuggestionDto>? ParseTaskSuggestions(string text)
        {
            var json = StripCodeFence(text);

            try
            {
                var payload = JsonSerializer.Deserialize<GeminiTaskSuggestionsPayload>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (payload?.Tasks is null)
                    return null;

                var today = DateTime.UtcNow.Date;

                return payload.Tasks
                    .Where(t => !string.IsNullOrWhiteSpace(t.Title))
                    .Take(8)
                    .Select(t =>
                    {
                        var title = t.Title!.Trim();
                        if (title.Length > 200)
                            title = title[..200];

                        var priority = Enum.TryParse<PriorityLevel>(t.Priority, ignoreCase: true, out var parsedPriority)
                            ? parsedPriority
                            : PriorityLevel.Medium;

                        var dueInDays = t.DueInDays.HasValue ? Math.Clamp(t.DueInDays.Value, 1, 60) : 7;

                        decimal? estimatedHours = t.EstimatedHours.HasValue && t.EstimatedHours.Value > 0
                            ? Math.Min(t.EstimatedHours.Value, 999)
                            : null;

                        return new AiTaskSuggestionDto
                        {
                            Title = title,
                            Description = string.IsNullOrWhiteSpace(t.Description) ? null : t.Description.Trim(),
                            DueDate = today.AddDays(dueInDays),
                            Priority = priority.ToString(),
                            EstimatedHours = estimatedHours
                        };
                    })
                    .ToList();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // Gemini sometimes wraps JSON in ```json ... ``` fences despite being
        // told not to — strip them rather than fail the whole request over it.
        private static string StripCodeFence(string text)
        {
            var trimmed = text.Trim();
            if (!trimmed.StartsWith("```"))
                return trimmed;

            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline >= 0)
                trimmed = trimmed[(firstNewline + 1)..];

            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0)
                trimmed = trimmed[..lastFence];

            return trimmed.Trim();
        }
    }
}

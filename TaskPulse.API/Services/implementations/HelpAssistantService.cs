using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.HelpDto;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    // The in-app "where do I find X" assistant. A real back-and-forth chat —
    // the frontend echoes back the conversation so far on every call, since
    // nothing is persisted server-side — grounded in a hand-written
    // description of the site's actual pages/actions so it can't hallucinate
    // features TaskPulse doesn't have.
    public class HelpAssistantService : IHelpAssistantService
    {
        private const int MaxHistoryTurns = 12;

        private readonly GeminiClient _geminiClient;

        public HelpAssistantService(GeminiClient geminiClient)
        {
            _geminiClient = geminiClient;
        }

        public async Task<CommonResponse<HelpAnswerDto>> AskAsync(AskHelpDto dto, int callerId, bool callerIsAdmin, string callerRole)
        {
            var question = dto.Question?.Trim() ?? string.Empty;

            if (question.Length == 0)
                return CommonResponseFactory.CreateErrorResponse<HelpAnswerDto>("Ask a question first.", HttpStatusCode.BadRequest);

            if (question.Length > 500)
                return CommonResponseFactory.CreateErrorResponse<HelpAnswerDto>("Keep the question to 500 characters or fewer.", HttpStatusCode.BadRequest);

            var systemInstruction = BuildSystemInstruction(dto.PageContext, callerRole);

            var turns = (dto.History ?? new List<HelpChatTurnDto>())
                .Where(t => !string.IsNullOrWhiteSpace(t.Text))
                .TakeLast(MaxHistoryTurns)
                .Select(t => (Role: t.Role == "model" ? "model" : "user", Text: t.Text.Length > 2000 ? t.Text[..2000] : t.Text))
                .Append((Role: "user", Text: question))
                .ToList();

            var text = await _geminiClient.GenerateChatAsync(systemInstruction, turns);
            if (text is null)
                return CommonResponseFactory.CreateErrorResponse<HelpAnswerDto>(
                    "The help assistant is temporarily unavailable. Please try again shortly.", HttpStatusCode.ServiceUnavailable);

            return CommonResponseFactory.CreateSuccessResponse(new HelpAnswerDto { Answer = text.Trim() }, "Answered.");
        }

        private static string BuildSystemInstruction(string? pageContext, string callerRole)
        {
            return $$"""
                You are the in-app help assistant for TaskPulse, a task/project management web app. You answer questions about how to use the site's UI — where to find a setting, how to do something — using ONLY the site description below. Never invent pages, buttons or features that aren't listed. The conversation may have earlier turns; use them for context on follow-up questions.

                The person asking is signed in as a {{callerRole}}.{{(string.IsNullOrWhiteSpace(pageContext) ? "" : $" They are currently on the page: {pageContext}.")}}

                === TaskPulse site map ===
                - Dashboard (/dashboard): landing page after login. Summary cards (project/task counts, overdue tasks, unread notifications, pending approvals) and two charts (tasks by status, tasks by priority).
                - Projects (/projects): list of projects, with search/filter. "New project" button (Admin/Manager only) opens a create form (name, description, status, priority, start/end date).
                - Project detail (/projects/{id}): project header with Edit button, an "AI Summary" button (AI-written status summary) and an "AI Risks" button (AI-generated potential-risks list with suggested actions), a member list with an "Add member" button (Admin/Manager), a task list/board, a Report section (completion %, required-file upload progress, charts, per-user performance table), and an Activity feed of recent changes.
                - Kanban board (within a project): tasks shown as cards grouped into columns by status (To Do, In Progress, Done). Drag a card between columns to change its status. Each card shows priority (a colored left bar), assignee, due date, and a small to-do checklist you can tick off (click the "+" on the card to add a to-do, max 30 characters; ticking items fills in the priority bar as progress).
                - Task detail (opened by clicking a task card): shows description, assignees, due date, estimated/actual hours, a required-file checklist (upload files against named requirements), a separate self-ticked to-do checklist, file attachments, and a comment thread. Buttons to submit the task for review or close it, depending on role.
                - Approvals (/approvals) — visible to Admins, and to anyone with a pending request: a queue of changes waiting on sign-off (project edits, task completions, reassignment requests). Approve or reject each one, with an optional note.
                - Notifications: a bell icon in the top bar. Click it to see recent notifications (assignments, approvals, deadlines); click a notification to jump to what it's about. Unread ones are highlighted.
                - Users (/users) — Admin only: a table of every user, with role, status (enabled/disabled), and attendance. "Create user" provisions a new account (the new user must confirm their email and set their own password). Click a user to see their attendance calendar.
                - Reports (/reports/progress): org-wide progress report — estimated vs. actual hours, deadline health (on track / at risk / late), and per-project completion.
                - Settings / profile: change your password, update your profile picture and details.
                - Attendance: a calendar showing clock-in/out records — your own from the Dashboard, or (Admin) any user's from their Users page entry.
                - Help: the "?" button floating in the bottom-right corner of every page opens this chat.

                Rules:
                - If the question is about something not covered above, say so plainly rather than guessing.
                - If a feature is role-restricted (e.g. Users, Approvals, project creation) and the asker's role likely can't use it, mention that.
                - Answer in 1-3 short sentences, plain text, no markdown, practical and direct (e.g. "Click the bell icon in the top bar, then click a notification to open what it's about.").
                """;
        }
    }
}

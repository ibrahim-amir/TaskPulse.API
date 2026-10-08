using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.Controllers.Infrastructure;
using TaskPulse.API.DTO.AiDto;
using TaskPulse.API.DTO.ApprovalDto;
using TaskPulse.API.DTO.ProjectDto;
using TaskPulse.API.DTO.ProjectMemberDto;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Controllers
{
    [Route("api/projects")]
    [ApiController]
    [Authorize]
    public class ProjectsController : BaseApiController
    {
        private readonly IProjectService _projectService;
        private readonly ITaskService _taskService;
        private readonly IAiInsightsService _aiInsightsService;

        public ProjectsController(IProjectService projectService, ITaskService taskService, IAiInsightsService aiInsightsService)
        {
            _projectService = projectService;
            _taskService = taskService;
            _aiInsightsService = aiInsightsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects([FromQuery] ProjectQueryDto query)
        {
            return Respond(await _projectService.GetProjectsAsync(query, User.GetUserId(), User.IsAdmin()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProjectById(int id)
        {
            return Respond(await _projectService.GetProjectByIdAsync(id, User.GetUserId(), User.IsAdmin()));
        }

        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Manager))]
        public async Task<IActionResult> CreateProject(CreateProjectDto dto)
        {
            return Respond(await _projectService.CreateProjectAsync(dto, User.GetUserId(), User.IsAdmin()));
        }

        // Open to any project member — ProjectService decides whether it
        // applies immediately (the project's creator, or an Admin) or has to
        // go through the creator-resolved edit-request gate instead.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, UpdateProjectDto dto)
        {
            //The id is the route's, never the body's.
            dto.Id = id;
            return Respond(await _projectService.UpdateProjectAsync(dto, User.GetUserId(), User.IsAdmin()));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Manager))]
        public async Task<IActionResult> DeleteProject(int id)
        {
            return Respond(await _projectService.DeleteProjectAsync(id, User.GetUserId(), User.IsAdmin()));
        }

        // This project's board. Same filters and paging as /api/tasks, just
        // pinned to one project.
        [HttpGet("{projectId}/tasks")]
        public async Task<IActionResult> GetProjectTasks(int projectId, [FromQuery] TaskQueryDto query)
        {
            return Respond(await _taskService.GetTasksAsync(query, projectId, User.GetUserId(), User.IsAdmin()));
        }

        // AI-generated status summary / risk scan — any project member can
        // ask for one, generated fresh each time rather than stored.
        [HttpGet("{id}/ai-summary")]
        public async Task<IActionResult> GetAiSummary(int id)
        {
            return Respond(await _aiInsightsService.GetProjectSummaryAsync(id, User.GetUserId(), User.IsAdmin()));
        }

        [HttpGet("{id}/ai-risks")]
        public async Task<IActionResult> GetAiRisks(int id)
        {
            return Respond(await _aiInsightsService.GetProjectRisksAsync(id, User.GetUserId(), User.IsAdmin()));
        }

        // A project-scoped chat grounded in the same live risk facts — "what
        // should I do next" type questions, answered specific to this project.
        [HttpPost("{id}/ai-insights/chat")]
        public async Task<IActionResult> AskProjectInsight(int id, ProjectAiChatRequestDto dto)
        {
            return Respond(await _aiInsightsService.AskProjectInsightAsync(id, User.GetUserId(), User.IsAdmin(), dto));
        }

        // Proposes tasks for review — from free text, or (blank) derived
        // from the project's current overdue/high-priority signals.
        [HttpPost("{id}/ai-insights/task-suggestions")]
        public async Task<IActionResult> GenerateTaskSuggestions(int id, GenerateTaskSuggestionsDto dto)
        {
            return Respond(await _aiInsightsService.GenerateTaskSuggestionsAsync(id, User.GetUserId(), User.IsAdmin(), dto));
        }

        // Creates the suggestions the user kept as real tasks (Manager/Admin
        // only, same as any other task creation).
        [HttpPost("{id}/ai-insights/tasks")]
        public async Task<IActionResult> AddAiTasks(int id, AddAiTasksDto dto)
        {
            return Respond(await _aiInsightsService.AddGeneratedTasksAsync(id, User.GetUserId(), User.IsAdmin(), dto));
        }

        [HttpGet("{projectId}/members")]
        public async Task<IActionResult> GetMembers(int projectId)
        {
            return Respond(await _projectService.GetMembersAsync(projectId, User.GetUserId(), User.IsAdmin()));
        }

        [HttpPost("{projectId}/members")]
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Manager))]
        public async Task<IActionResult> AddMember(int projectId, AddProjectMemberDto dto)
        {
            dto.ProjectId = projectId;
            return Respond(await _projectService.AddMemberAsync(dto, User.GetUserId(), User.IsAdmin()));
        }

        [HttpDelete("{projectId}/members/{userId}")]
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Manager))]
        public async Task<IActionResult> RemoveMember(int projectId, int userId)
        {
            return Respond(await _projectService.RemoveMemberAsync(projectId, userId, User.GetUserId(), User.IsAdmin()));
        }


        // --- Edit requests: resolved by the project's creator, not an Admin ---

        [HttpGet("edit-requests/pending-for-me")]
        public async Task<IActionResult> GetMyPendingProjectEditRequests()
        {
            return Respond(await _projectService.GetPendingEditRequestsForCreatorAsync(User.GetUserId()));
        }

        [HttpGet("edit-requests/sent-by-me")]
        public async Task<IActionResult> GetMySentProjectEditRequests()
        {
            return Respond(await _projectService.GetSentEditRequestsAsync(User.GetUserId()));
        }

        [HttpPost("edit-requests/{requestId}/approve")]
        public async Task<IActionResult> ApproveProjectEditRequest(int requestId, ResolveApprovalDto dto)
        {
            return Respond(await _projectService.RespondToEditRequestAsync(requestId, User.GetUserId(), User.IsAdmin(), approve: true, dto.Note));
        }

        [HttpPost("edit-requests/{requestId}/reject")]
        public async Task<IActionResult> RejectProjectEditRequest(int requestId, ResolveApprovalDto dto)
        {
            return Respond(await _projectService.RespondToEditRequestAsync(requestId, User.GetUserId(), User.IsAdmin(), approve: false, dto.Note));
        }
    }
}

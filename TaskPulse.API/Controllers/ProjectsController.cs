using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.Controllers.Infrastructure;
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

        public ProjectsController(IProjectService projectService, ITaskService taskService)
        {
            _projectService = projectService;
            _taskService = taskService;
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

        [HttpPut("{id}")]
        [Authorize(Roles = nameof(UserRole.Admin) + "," + nameof(UserRole.Manager))]
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
    }
}

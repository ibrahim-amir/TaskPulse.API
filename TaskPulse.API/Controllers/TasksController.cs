using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.Controllers.Infrastructure;
using TaskPulse.API.DTO.SubtaskDto;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Controllers
{
    [Route("api/tasks")]
    [ApiController]
    [Authorize]
    public class TasksController : BaseApiController
    {
        private const string ManagerOrAdmin = nameof(UserRole.Admin) + "," + nameof(UserRole.Manager);

        private readonly ITaskService _taskService;
        private readonly ITaskFileService _taskFileService;

        public TasksController(ITaskService taskService, ITaskFileService taskFileService)
        {
            _taskService = taskService;
            _taskFileService = taskFileService;
        }

        // Across every project the caller can see — "my tasks" and the board
        // both come from here with different filters.
        [HttpGet]
        public async Task<IActionResult> GetTasks([FromQuery] TaskQueryDto query)
        {
            return Respond(await _taskService.GetTasksAsync(query, null, User.GetUserId(), User.IsAdmin()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            return Respond(await _taskService.GetTaskByIdAsync(id, User.GetUserId(), User.IsAdmin()));
        }

        [HttpPost]
        [Authorize(Roles = ManagerOrAdmin)]
        public async Task<IActionResult> CreateTask(CreateTaskDto dto)
        {
            return Respond(await _taskService.CreateTaskAsync(dto, User.GetUserId(), User.IsAdmin()));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = ManagerOrAdmin)]
        public async Task<IActionResult> UpdateTask(int id, UpdateTaskDto dto)
        {
            //The id is the route's, never the body's.
            dto.Id = id;
            return Respond(await _taskService.UpdateTaskAsync(dto, User.GetUserId(), User.IsAdmin()));
        }

        // The Kanban drag. Open to an assigned Member on purpose — moving a card
        // is the one change they're allowed to make.
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(int id, UpdateTaskStatusDto dto)
        {
            dto.Id = id;
            return Respond(await _taskService.UpdateTaskStatusAsync(dto, User.GetUserId(), User.IsAdmin()));
        }

        [HttpPut("{id}/assignees")]
        [Authorize(Roles = ManagerOrAdmin)]
        public async Task<IActionResult> AssignTask(int id, AssignTaskDto dto)
        {
            dto.TaskId = id;
            return Respond(await _taskService.AssignTaskAsync(dto, User.GetUserId(), User.IsAdmin()));
        }

        // Review passed — marks it Done and stops further uploads.
        [HttpPost("{id}/close")]
        [Authorize(Roles = ManagerOrAdmin)]
        public async Task<IActionResult> CloseTask(int id)
        {
            return Respond(await _taskService.CloseTaskAsync(id, User.GetUserId(), User.IsAdmin()));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = ManagerOrAdmin)]
        public async Task<IActionResult> DeleteTask(int id)
        {
            return Respond(await _taskService.DeleteTaskAsync(id, User.GetUserId(), User.IsAdmin()));
        }


        // --- The required-file checklist ---

        [HttpGet("{taskId}/subtasks")]
        public async Task<IActionResult> GetSubtasks(int taskId)
        {
            return Respond(await _taskFileService.GetSubtasksAsync(taskId, User.GetUserId(), User.IsAdmin()));
        }

        [HttpPost("{taskId}/subtasks")]
        [Authorize(Roles = ManagerOrAdmin)]
        public async Task<IActionResult> AddSubtask(int taskId, CreateSubtaskDto dto)
        {
            dto.TaskId = taskId;
            return Respond(await _taskFileService.AddSubtaskAsync(dto, User.GetUserId(), User.IsAdmin()));
        }

        [HttpDelete("{taskId}/subtasks/{subtaskId}")]
        [Authorize(Roles = ManagerOrAdmin)]
        public async Task<IActionResult> RemoveSubtask(int taskId, int subtaskId)
        {
            return Respond(await _taskFileService.RemoveSubtaskAsync(taskId, subtaskId, User.GetUserId(), User.IsAdmin()));
        }

        // Fills one checklist slot. Multipart, so the file comes in as a form
        // field rather than JSON.
        [HttpPost("{taskId}/subtasks/{subtaskId}/upload")]
        public async Task<IActionResult> UploadSubtaskFile(int taskId, int subtaskId, IFormFile file)
        {
            return Respond(await _taskFileService.UploadSubtaskFileAsync(taskId, subtaskId, file, User.GetUserId(), User.IsAdmin()));
        }


        // --- Attachments not tied to a checklist slot ---

        [HttpGet("{taskId}/attachments")]
        public async Task<IActionResult> GetAttachments(int taskId)
        {
            return Respond(await _taskFileService.GetAttachmentsAsync(taskId, User.GetUserId(), User.IsAdmin()));
        }

        [HttpPost("{taskId}/attachments")]
        public async Task<IActionResult> UploadAttachment(int taskId, IFormFile file)
        {
            return Respond(await _taskFileService.UploadAttachmentAsync(taskId, file, User.GetUserId(), User.IsAdmin()));
        }

        // The one endpoint that doesn't return a CommonResponse on success —
        // the caller wants the actual bytes, not an envelope around them.
        [HttpGet("{taskId}/attachments/{attachmentId}/download")]
        public async Task<IActionResult> DownloadAttachment(int taskId, int attachmentId)
        {
            var result = await _taskFileService.DownloadAttachmentAsync(taskId, attachmentId, User.GetUserId(), User.IsAdmin());

            if (!result.Success || result.Data is null)
                return Respond(result);

            return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
        }

        [HttpDelete("{taskId}/attachments/{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(int taskId, int attachmentId)
        {
            return Respond(await _taskFileService.DeleteAttachmentAsync(taskId, attachmentId, User.GetUserId(), User.IsAdmin()));
        }
    }
}

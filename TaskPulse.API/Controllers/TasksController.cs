using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.Controllers.Infrastructure;
using TaskPulse.API.DTO.ApprovalDto;
using TaskPulse.API.DTO.SubtaskDto;
using TaskPulse.API.DTO.TaskApprovalDto;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.DTO.TaskTodoDto;
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

        // Open to any project member — a Manager/Admin's copy is added right
        // away; anyone else's is queued for that project's Manager (or an
        // Admin) to approve first, via the duplicate-requests endpoints below.
        [HttpPost("{id}/duplicate")]
        public async Task<IActionResult> DuplicateTask(int id, CreateTaskDto dto)
        {
            return Respond(await _taskService.DuplicateTaskAsync(id, dto, User.GetUserId(), User.IsAdmin()));
        }

        // Open to any project member — TaskService decides whether it applies
        // immediately (the task's creator, or an Admin) or has to go through
        // the creator-resolved edit-request gate instead.
        [HttpPut("{id}")]
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

        // The Member-facing path: files an approval request for the Admin
        // instead of closing the task directly.
        [HttpPost("{id}/submit-for-review")]
        public async Task<IActionResult> SubmitForReview(int id)
        {
            return Respond(await _taskService.SubmitForReviewAsync(id, User.GetUserId(), User.IsAdmin()));
        }

        // Open to any project member — TaskService decides whether it applies
        // immediately (the task's creator, or an Admin) or has to go through
        // the creator-resolved deletion-request gate instead.
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            return Respond(await _taskService.DeleteTaskAsync(id, User.GetUserId(), User.IsAdmin()));
        }


        // --- Tagged multi-approver submission workflow ---

        [HttpPost("{id}/approval-requests")]
        public async Task<IActionResult> RequestTaskApproval(int id, CreateTaskApprovalRequestDto dto)
        {
            return Respond(await _taskService.RequestTaskApprovalAsync(id, User.GetUserId(), User.IsAdmin(), dto));
        }

        [HttpGet("{id}/approval-requests/current")]
        public async Task<IActionResult> GetCurrentTaskApproval(int id)
        {
            return Respond(await _taskService.GetTaskApprovalStatusAsync(id, User.GetUserId(), User.IsAdmin()));
        }

        // Every open request tagging the caller as an approver, across every
        // task — the Approvals page's "Waiting on you" section.
        [HttpGet("approval-requests/pending-for-me")]
        public async Task<IActionResult> GetMyPendingTaskApprovals()
        {
            return Respond(await _taskService.GetMyPendingTaskApprovalsAsync(User.GetUserId()));
        }

        // Every request the caller has filed, any status — the Approvals
        // page's "Sent by me" tab.
        [HttpGet("approval-requests/sent-by-me")]
        public async Task<IActionResult> GetMySentTaskApprovals()
        {
            return Respond(await _taskService.GetMySentTaskApprovalsAsync(User.GetUserId()));
        }

        // Scoped by the caller's own id, not a route parameter — each tagged
        // approver can only ever answer for themselves.
        [HttpPost("approval-requests/{requestId}/respond")]
        public async Task<IActionResult> RespondToTaskApproval(int requestId, RespondToTaskApprovalDto dto)
        {
            return Respond(await _taskService.RespondToTaskApprovalAsync(requestId, User.GetUserId(), dto));
        }

        [HttpPost("{id}/submit")]
        public async Task<IActionResult> SubmitApprovedTask(int id)
        {
            return Respond(await _taskService.SubmitApprovedTaskAsync(id, User.GetUserId(), User.IsAdmin()));
        }

        [HttpPost("{id}/reopen")]
        public async Task<IActionResult> ReopenTask(int id)
        {
            return Respond(await _taskService.ReopenTaskAsync(id, User.GetUserId(), User.IsAdmin()));
        }

        // Files a real, Admin-resolved ApprovalRequest asking to be added to
        // this task — shows up on the Approvals page, not as a chat message.
        [HttpPost("{id}/request-assignment")]
        public async Task<IActionResult> RequestAssignment(int id)
        {
            return Respond(await _taskService.RequestAssignmentAsync(id, User.GetUserId()));
        }


        // --- Edit requests: resolved by the task's creator, not an Admin ---

        [HttpGet("edit-requests/pending-for-me")]
        public async Task<IActionResult> GetMyPendingTaskEditRequests()
        {
            return Respond(await _taskService.GetPendingEditRequestsForCreatorAsync(User.GetUserId()));
        }

        [HttpGet("edit-requests/sent-by-me")]
        public async Task<IActionResult> GetMySentTaskEditRequests()
        {
            return Respond(await _taskService.GetSentEditRequestsAsync(User.GetUserId()));
        }

        [HttpPost("edit-requests/{requestId}/approve")]
        public async Task<IActionResult> ApproveTaskEditRequest(int requestId, ResolveApprovalDto dto)
        {
            return Respond(await _taskService.RespondToEditRequestAsync(requestId, User.GetUserId(), User.IsAdmin(), approve: true, dto.Note));
        }

        [HttpPost("edit-requests/{requestId}/reject")]
        public async Task<IActionResult> RejectTaskEditRequest(int requestId, ResolveApprovalDto dto)
        {
            return Respond(await _taskService.RespondToEditRequestAsync(requestId, User.GetUserId(), User.IsAdmin(), approve: false, dto.Note));
        }


        // --- Duplicate requests: resolved by a Manager/Admin of the source task's project ---

        [HttpGet("duplicate-requests/pending-for-me")]
        public async Task<IActionResult> GetMyPendingDuplicateRequests()
        {
            return Respond(await _taskService.GetPendingDuplicateRequestsForManagerAsync(User.GetUserId(), User.IsAdmin()));
        }

        [HttpGet("duplicate-requests/sent-by-me")]
        public async Task<IActionResult> GetMySentDuplicateRequests()
        {
            return Respond(await _taskService.GetSentDuplicateRequestsAsync(User.GetUserId()));
        }

        [HttpPost("duplicate-requests/{requestId}/approve")]
        public async Task<IActionResult> ApproveDuplicateRequest(int requestId, ResolveApprovalDto dto)
        {
            return Respond(await _taskService.RespondToDuplicateRequestAsync(requestId, User.GetUserId(), User.IsAdmin(), approve: true, dto.Note));
        }

        [HttpPost("duplicate-requests/{requestId}/reject")]
        public async Task<IActionResult> RejectDuplicateRequest(int requestId, ResolveApprovalDto dto)
        {
            return Respond(await _taskService.RespondToDuplicateRequestAsync(requestId, User.GetUserId(), User.IsAdmin(), approve: false, dto.Note));
        }


        // --- Busy-user assignment requests: resolved by the target user, or an Admin ---

        [HttpGet("assignment-requests/pending-for-me")]
        public async Task<IActionResult> GetMyPendingBusyAssignmentRequests()
        {
            return Respond(await _taskService.GetPendingBusyAssignmentsForMeAsync(User.GetUserId()));
        }

        [HttpGet("assignment-requests/sent-by-me")]
        public async Task<IActionResult> GetMySentBusyAssignmentRequests()
        {
            return Respond(await _taskService.GetSentBusyAssignmentRequestsAsync(User.GetUserId()));
        }

        [HttpPost("assignment-requests/{requestId}/approve")]
        public async Task<IActionResult> ApproveBusyAssignmentRequest(int requestId, ResolveApprovalDto dto)
        {
            return Respond(await _taskService.RespondToBusyAssignmentAsync(requestId, User.GetUserId(), User.IsAdmin(), approve: true, dto.Note));
        }

        [HttpPost("assignment-requests/{requestId}/reject")]
        public async Task<IActionResult> RejectBusyAssignmentRequest(int requestId, ResolveApprovalDto dto)
        {
            return Respond(await _taskService.RespondToBusyAssignmentAsync(requestId, User.GetUserId(), User.IsAdmin(), approve: false, dto.Note));
        }


        // --- To-do items: a separate, self-ticked checklist — no files ---

        [HttpPost("{taskId}/todos")]
        public async Task<IActionResult> AddTodo(int taskId, CreateTaskTodoItemDto dto)
        {
            dto.TaskId = taskId;
            return Respond(await _taskService.AddTodoAsync(taskId, User.GetUserId(), User.IsAdmin(), dto));
        }

        [HttpPatch("{taskId}/todos/{todoId}/toggle")]
        public async Task<IActionResult> ToggleTodo(int taskId, int todoId)
        {
            return Respond(await _taskService.ToggleTodoAsync(taskId, todoId, User.GetUserId(), User.IsAdmin()));
        }

        [HttpDelete("{taskId}/todos/{todoId}")]
        public async Task<IActionResult> DeleteTodo(int taskId, int todoId)
        {
            return Respond(await _taskService.DeleteTodoAsync(taskId, todoId, User.GetUserId(), User.IsAdmin()));
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

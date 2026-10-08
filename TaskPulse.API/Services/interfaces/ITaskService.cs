using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.ApprovalDto;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.TaskApprovalDto;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.DTO.TaskTodoDto;

namespace TaskPulse.API.Services.interfaces
{
    public interface ITaskService
    {
        Task<CommonResponse<PagedResult<TaskDTO>>> GetTasksAsync(TaskQueryDto query, int? projectId, int callerId, bool callerIsAdmin);

        Task<CommonResponse<TaskDTO>> GetTaskByIdAsync(int id, int callerId, bool callerIsAdmin);

        Task<CommonResponse<TaskDTO>> CreateTaskAsync(CreateTaskDto dto, int callerId, bool callerIsAdmin);

        // Duplicates an existing, already-visible task — open to any project
        // member, unlike CreateTaskAsync's blank "New task" (Manager/Admin only).
        Task<CommonResponse<TaskDTO>> DuplicateTaskAsync(int sourceTaskId, CreateTaskDto dto, int callerId, bool callerIsAdmin);

        Task<CommonResponse<TaskDTO>> UpdateTaskAsync(UpdateTaskDto dto, int callerId, bool callerIsAdmin);

        // The Kanban move. Separate from UpdateTaskAsync because an assigned
        // Member is allowed to do this and nothing else.
        Task<CommonResponse<TaskDTO>> UpdateTaskStatusAsync(UpdateTaskStatusDto dto, int callerId, bool callerIsAdmin);

        Task<CommonResponse<TaskDTO>> AssignTaskAsync(AssignTaskDto dto, int callerId, bool callerIsAdmin);

        // Review passed: marks the task Done and stops further uploads.
        Task<CommonResponse<TaskDTO>> CloseTaskAsync(int id, int callerId, bool callerIsAdmin);

        // The Member-facing path to the same outcome as CloseTaskAsync — files
        // an approval request instead of closing the task directly, since a
        // Member (unlike a Manager/Admin) can't just decide the work is done.
        Task<CommonResponse<string>> SubmitForReviewAsync(int id, int callerId, bool callerIsAdmin);

        Task<CommonResponse<string>> DeleteTaskAsync(int id, int callerId, bool callerIsAdmin);

        // Tagged multi-approver submission workflow — every listed approver
        // has to sign off before SubmitApprovedTaskAsync is allowed to run.
        Task<CommonResponse<TaskApprovalRequestDTO>> RequestTaskApprovalAsync(int taskId, int callerId, bool callerIsAdmin, CreateTaskApprovalRequestDto dto);

        Task<CommonResponse<TaskApprovalRequestDTO?>> GetTaskApprovalStatusAsync(int taskId, int callerId, bool callerIsAdmin);

        Task<CommonResponse<TaskApprovalRequestDTO>> RespondToTaskApprovalAsync(int requestId, int callerId, RespondToTaskApprovalDto dto);

        Task<CommonResponse<string>> SubmitApprovedTaskAsync(int taskId, int callerId, bool callerIsAdmin);

        // Undoes CloseTaskAsync/SubmitApprovedTaskAsync — clears CompletedDate,
        // moves the task back to InProgress, and retires any stale Approved
        // request so a fresh approval cycle is required before it can be
        // submitted again.
        Task<CommonResponse<TaskDTO>> ReopenTaskAsync(int taskId, int callerId, bool callerIsAdmin);

        // Someone not currently on a task asking to be added to it — a real,
        // Admin-resolved ApprovalRequest (the generic gate) rather than a
        // plain chat notification, so it shows up on the Approvals page.
        Task<CommonResponse<string>> RequestAssignmentAsync(int taskId, int callerId);

        // Every open request tagging the caller as an approver, across every
        // task — what the Approvals page shows under "Waiting on you".
        Task<CommonResponse<List<TaskApprovalRequestDTO>>> GetMyPendingTaskApprovalsAsync(int callerId);

        // Every request the caller has filed, any status — the Approvals
        // page's "Sent by me" tab.
        Task<CommonResponse<List<TaskApprovalRequestDTO>>> GetMySentTaskApprovalsAsync(int callerId);

        // --- Edit requests: resolved by the task's creator, not an Admin ---

        Task<CommonResponse<List<ApprovalRequestDTO>>> GetPendingEditRequestsForCreatorAsync(int callerId);

        Task<CommonResponse<List<ApprovalRequestDTO>>> GetSentEditRequestsAsync(int callerId);

        Task<CommonResponse<ApprovalRequestDTO>> RespondToEditRequestAsync(int requestId, int callerId, bool callerIsAdmin, bool approve, string? note);

        // --- Duplicate requests: resolved by a Manager/Admin of the source task's project ---

        Task<CommonResponse<List<ApprovalRequestDTO>>> GetPendingDuplicateRequestsForManagerAsync(int callerId, bool callerIsAdmin);

        Task<CommonResponse<List<ApprovalRequestDTO>>> GetSentDuplicateRequestsAsync(int callerId);

        Task<CommonResponse<ApprovalRequestDTO>> RespondToDuplicateRequestAsync(int requestId, int callerId, bool callerIsAdmin, bool approve, string? note);

        // --- Busy-user assignment requests: resolved by the target user, or an Admin ---

        Task<CommonResponse<List<ApprovalRequestDTO>>> GetPendingBusyAssignmentsForMeAsync(int callerId);

        Task<CommonResponse<List<ApprovalRequestDTO>>> GetSentBusyAssignmentRequestsAsync(int callerId);

        Task<CommonResponse<ApprovalRequestDTO>> RespondToBusyAssignmentAsync(int requestId, int callerId, bool callerIsAdmin, bool approve, string? note);

        // --- To-do items: a separate, self-ticked checklist — no files ---

        Task<CommonResponse<TaskTodoItemDTO>> AddTodoAsync(int taskId, int callerId, bool callerIsAdmin, CreateTaskTodoItemDto dto);

        Task<CommonResponse<TaskTodoItemDTO>> ToggleTodoAsync(int taskId, int todoId, int callerId, bool callerIsAdmin);

        Task<CommonResponse<string>> DeleteTodoAsync(int taskId, int todoId, int callerId, bool callerIsAdmin);
    }
}

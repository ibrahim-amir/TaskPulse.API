using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.TaskDto;

namespace TaskPulse.API.Services.interfaces
{
    public interface ITaskService
    {
        Task<CommonResponse<PagedResult<TaskDTO>>> GetTasksAsync(TaskQueryDto query, int? projectId, int callerId, bool callerIsAdmin);

        Task<CommonResponse<TaskDTO>> GetTaskByIdAsync(int id, int callerId, bool callerIsAdmin);

        Task<CommonResponse<TaskDTO>> CreateTaskAsync(CreateTaskDto dto, int callerId, bool callerIsAdmin);

        Task<CommonResponse<TaskDTO>> UpdateTaskAsync(UpdateTaskDto dto, int callerId, bool callerIsAdmin);

        // The Kanban move. Separate from UpdateTaskAsync because an assigned
        // Member is allowed to do this and nothing else.
        Task<CommonResponse<TaskDTO>> UpdateTaskStatusAsync(UpdateTaskStatusDto dto, int callerId, bool callerIsAdmin);

        Task<CommonResponse<TaskDTO>> AssignTaskAsync(AssignTaskDto dto, int callerId, bool callerIsAdmin);

        // Review passed: marks the task Done and stops further uploads.
        Task<CommonResponse<TaskDTO>> CloseTaskAsync(int id, int callerId, bool callerIsAdmin);

        Task<CommonResponse<string>> DeleteTaskAsync(int id, int callerId, bool callerIsAdmin);
    }
}

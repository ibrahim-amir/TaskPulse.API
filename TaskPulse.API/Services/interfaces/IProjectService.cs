using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.ApprovalDto;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.ProjectDto;
using TaskPulse.API.DTO.ProjectMemberDto;

namespace TaskPulse.API.Services.interfaces
{
    // callerId/callerIsAdmin come from the token, via the controller. The
    // service needs both to decide what the caller can see and whether a change
    // has to go past the Admin first.
    public interface IProjectService
    {
        Task<CommonResponse<PagedResult<ProjectDTO>>> GetProjectsAsync(ProjectQueryDto query, int callerId, bool callerIsAdmin);

        // Every project a given user belongs to — for their profile page.
        // Open to any authenticated caller, same as GET /api/users/{id}.
        Task<CommonResponse<PagedResult<ProjectDTO>>> GetProjectsForUserAsync(int userId);

        Task<CommonResponse<ProjectDTO>> GetProjectByIdAsync(int id, int callerId, bool callerIsAdmin);

        Task<CommonResponse<ProjectDTO>> CreateProjectAsync(CreateProjectDto dto, int callerId, bool callerIsAdmin);

        Task<CommonResponse<ProjectDTO>> UpdateProjectAsync(UpdateProjectDto dto, int callerId, bool callerIsAdmin);

        Task<CommonResponse<string>> DeleteProjectAsync(int id, int callerId, bool callerIsAdmin);

        Task<CommonResponse<List<ProjectMemberDTO>>> GetMembersAsync(int projectId, int callerId, bool callerIsAdmin);

        Task<CommonResponse<ProjectMemberDTO>> AddMemberAsync(AddProjectMemberDto dto, int callerId, bool callerIsAdmin);

        Task<CommonResponse<string>> RemoveMemberAsync(int projectId, int userId, int callerId, bool callerIsAdmin);

        // --- Edit requests: resolved by the project's creator, not an Admin ---

        Task<CommonResponse<List<ApprovalRequestDTO>>> GetPendingEditRequestsForCreatorAsync(int callerId);

        Task<CommonResponse<List<ApprovalRequestDTO>>> GetSentEditRequestsAsync(int callerId);

        Task<CommonResponse<ApprovalRequestDTO>> RespondToEditRequestAsync(int requestId, int callerId, bool callerIsAdmin, bool approve, string? note);
    }
}

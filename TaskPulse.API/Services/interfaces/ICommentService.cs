using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.CommentDto;

namespace TaskPulse.API.Services.interfaces
{
    public interface ICommentService
    {
        Task<CommonResponse<List<CommentDTO>>> GetCommentsAsync(int taskId, int callerId, bool callerIsAdmin);

        Task<CommonResponse<CommentDTO>> AddCommentAsync(CreateCommentDto dto, int callerId, bool callerIsAdmin);

        Task<CommonResponse<CommentDTO>> UpdateCommentAsync(UpdateCommentDto dto, int callerId, bool callerIsAdmin);

        Task<CommonResponse<string>> DeleteCommentAsync(int id, int callerId, bool callerIsAdmin);
    }
}

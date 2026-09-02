using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.Controllers.Infrastructure;
using TaskPulse.API.DTO.CommentDto;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Controllers
{
    // Reading and writing comments hangs off a task; editing and deleting act
    // on the comment itself, which is why the routes split the way they do.
    [ApiController]
    [Authorize]
    public class CommentsController : BaseApiController
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpGet("api/tasks/{taskId}/comments")]
        public async Task<IActionResult> GetComments(int taskId)
        {
            return Respond(await _commentService.GetCommentsAsync(taskId, User.GetUserId(), User.IsAdmin()));
        }

        [HttpPost("api/tasks/{taskId}/comments")]
        public async Task<IActionResult> AddComment(int taskId, CreateCommentDto dto)
        {
            //The id is the route's, never the body's.
            dto.TaskId = taskId;
            return Respond(await _commentService.AddCommentAsync(dto, User.GetUserId(), User.IsAdmin()));
        }

        [HttpPut("api/comments/{id}")]
        public async Task<IActionResult> UpdateComment(int id, UpdateCommentDto dto)
        {
            dto.Id = id;
            return Respond(await _commentService.UpdateCommentAsync(dto, User.GetUserId(), User.IsAdmin()));
        }

        [HttpDelete("api/comments/{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            return Respond(await _commentService.DeleteCommentAsync(id, User.GetUserId(), User.IsAdmin()));
        }
    }
}

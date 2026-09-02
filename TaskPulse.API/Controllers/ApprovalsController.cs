using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.Controllers.Infrastructure;
using TaskPulse.API.DTO.ApprovalDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Controllers
{
    // The Admin's queue. Everything here is Admin-only by definition — the
    // whole point is that nobody else gets to answer these.
    [Route("api/approvals")]
    [ApiController]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public class ApprovalsController : BaseApiController
    {
        private readonly IApprovalService _approvalService;

        public ApprovalsController(IApprovalService approvalService)
        {
            _approvalService = approvalService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRequests([FromQuery] ApprovalQueryDto query)
        {
            return Respond(await _approvalService.GetRequestsAsync(query));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequestById(int id)
        {
            return Respond(await _approvalService.GetByIdAsync(id));
        }

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id, ResolveApprovalDto dto)
        {
            return Respond(await _approvalService.ApproveAsync(id, User.GetUserId(), dto));
        }

        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(int id, ResolveApprovalDto dto)
        {
            return Respond(await _approvalService.RejectAsync(id, User.GetUserId(), dto));
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.Controllers.Infrastructure;
using TaskPulse.API.DTO.HelpDto;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Controllers
{
    // The site-wide "how do I..." help assistant — a single ask/answer
    // endpoint, not a stored conversation.
    [Route("api/help")]
    [ApiController]
    [Authorize]
    public class HelpController : BaseApiController
    {
        private readonly IHelpAssistantService _helpAssistantService;

        public HelpController(IHelpAssistantService helpAssistantService)
        {
            _helpAssistantService = helpAssistantService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask(AskHelpDto dto)
        {
            return Respond(await _helpAssistantService.AskAsync(dto, User.GetUserId(), User.IsAdmin(), User.GetRole()));
        }
    }
}

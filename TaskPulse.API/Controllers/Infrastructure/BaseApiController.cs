using System.Net;
using Microsoft.AspNetCore.Mvc;
using TaskPulse.API.CommonResponseClass;

namespace TaskPulse.API.Controllers.Infrastructure
{
    // Same idea as BaseServiceClass and BaseRepositoryClass: the bit every
    // controller repeats, in one place. Controllers stay DTO in, DTO out.
    public abstract class BaseApiController : ControllerBase
    {
        // Every service returns a CommonResponse that already carries its own
        // status code, so the controller just hands it back with that code.
        protected IActionResult Respond<T>(CommonResponse<T> response)
        {
            return StatusCode((int)response.StatusCode, response);
        }

        // The caller is authenticated but not allowed to touch this record.
        protected IActionResult Forbidden(string message)
        {
            return Respond(CommonResponseFactory.CreateErrorResponse<string>(message, HttpStatusCode.Forbidden));
        }
    }
}

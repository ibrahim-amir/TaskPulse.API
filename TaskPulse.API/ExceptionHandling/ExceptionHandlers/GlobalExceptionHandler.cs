using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;

namespace TaskPulse.API.ExceptionHandling.ExceptionHandlers
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            await httpContext.Response.WriteAsJsonAsync(CommonResponseFactory.CreateErrorResponse<object>(CommonResponseMessage.ServerError.ToText(), HttpStatusCode.InternalServerError), cancellationToken);

            return true;
        }
    }
}

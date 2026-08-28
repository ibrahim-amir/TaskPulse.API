
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Net;
using System.Text.Json;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers.CustomeExceptionHandler;

namespace TaskPulse.API.ExceptionHandling.ExceptionHandlers
{

    public class CustomExceptionHandler(ILogger<CustomExceptionHandler> logger) : ICustomExceptionHandler
    {
        public async Task<CommonResponse<T>> HandleAsync<T>(Exception ex, string context,CommonResponse<T>? response = null)
        {
            var resp = response ?? new CommonResponse<T>();
            resp.Success = false;
            resp.StatusCode = ex switch
            {

                FormatException => HttpStatusCode.BadRequest,

                JsonException => HttpStatusCode.BadRequest,

                TaskCanceledException => HttpStatusCode.RequestTimeout,

                OperationCanceledException => HttpStatusCode.RequestTimeout,

                InvalidOperationException => HttpStatusCode.InternalServerError,

                HttpRequestException => HttpStatusCode.InternalServerError,

                DbException => HttpStatusCode.InternalServerError,

                _ => HttpStatusCode.InternalServerError
            };

            return resp;
        }

    }
}

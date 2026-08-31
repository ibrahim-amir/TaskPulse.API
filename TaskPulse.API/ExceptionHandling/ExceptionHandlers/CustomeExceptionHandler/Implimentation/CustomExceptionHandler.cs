using System.Data.Common;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers.CustomeExceptionHandler;
using TaskPulse.API.ExceptionHandling.Exceptions;

namespace TaskPulse.API.ExceptionHandling.ExceptionHandlers
{

    public class CustomExceptionHandler(ILogger<CustomExceptionHandler> logger) : ICustomExceptionHandler
    {
        public async Task<CommonResponse<T>> HandleAsync<T>(Exception ex, string context,CommonResponse<T>? response = null)
        {
            var resp = response ?? new CommonResponse<T>();

            var (statusCode, message, errorCode) = ex switch
            {
                DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "This record was already modified or deleted by someone else.", "DB_CONCURRENCY_CONFLICT"),

                DbUpdateException => (HttpStatusCode.Conflict, "The change could not be saved because it conflicts with existing data.", "DB_UPDATE_FAILED"),

                DbException => (HttpStatusCode.InternalServerError, "A database error occurred while processing the request.", "DB_ERROR"),

                DataNotFound => (HttpStatusCode.NotFound, ex.Message, "NOT_FOUND"),

                FormatException => (HttpStatusCode.BadRequest, "The request data was not in the expected format.", "FORMAT_ERROR"),

                JsonException => (HttpStatusCode.BadRequest, "The request body could not be parsed as valid JSON.", "JSON_PARSE_ERROR"),

                TaskCanceledException => (HttpStatusCode.RequestTimeout, "The request took too long and was cancelled.", "REQUEST_TIMEOUT"),

                OperationCanceledException => (HttpStatusCode.RequestTimeout, "The operation was cancelled.", "OPERATION_CANCELLED"),

                NotSupportedException => (HttpStatusCode.InternalServerError, "The server attempted an unsupported operation.", "NOT_SUPPORTED"),

                InvalidOperationException => (HttpStatusCode.InternalServerError, "The request could not be completed due to the application's current state.", "INVALID_OPERATION"),

                HttpRequestException => (HttpStatusCode.InternalServerError, "A downstream request failed.", "HTTP_REQUEST_FAILED"),

                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", "UNKNOWN_ERROR")
            };

            var traceId = Guid.NewGuid().ToString();

            logger.LogError(ex, "Unhandled exception in {Context} (TraceId: {TraceId}, ErrorCode: {ErrorCode})", context, traceId, errorCode);

            resp.Success = false;
            resp.StatusCode = statusCode;
            resp.ErrorCode = errorCode;
            resp.TraceId = traceId;
            resp.Errors.Add(message);

            return resp;
        }

    }
}

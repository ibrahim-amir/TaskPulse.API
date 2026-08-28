using System.Net;

namespace TaskPulse.API.CommonResponseClass
{
    public class CommonResponse<T>
    {
        public bool Success { get; set; }
        public List<string> Messages { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public T? Data { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
    public static class CommonResponseFactory
    {
        public static CommonResponse<T> CreateSuccessResponse<T>(T data, List<string>? messages = null)
        {
            return new CommonResponse<T>
            {
                Success = true,
                Messages = messages ?? new List<string>(),
                Data = data,
                StatusCode = HttpStatusCode.OK
            };
        }

        public static CommonResponse<T> CreateSuccessResponse<T>(T data, string message)
        {
            return new CommonResponse<T>
            {
                Success = true,
                Messages = new List<string> { message },
                Data = data,
                StatusCode = HttpStatusCode.OK
            };
        }

        public static CommonResponse<T> CreateErrorResponse<T>(List<string> errors, HttpStatusCode statusCode)
        {
            return new CommonResponse<T>
            {
                Success = false,
                Errors = errors ?? new List<string>(),
                Data = default,
                StatusCode = statusCode
            };
        }

        public static CommonResponse<T> CreateErrorResponse<T>(string error, HttpStatusCode statusCode)
        {
            return new CommonResponse<T>
            {
                Success = false,
                Errors = new List<string> { error },
                Data = default,
                StatusCode = statusCode
            };
        }

        public static CommonResponse<T> ReturnError<T>(this CommonResponse<T> response, HttpStatusCode statusCode, string errorMessage)
        {
            response.Success = false;
            response.Errors.Add(errorMessage);
            response.StatusCode = statusCode;
            return response;
        }
    }
}



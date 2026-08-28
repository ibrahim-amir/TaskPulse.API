using TaskPulse.API.CommonResponseClass;

namespace TaskPulse.API.ExceptionHandling.ExceptionHandlers.CustomeExceptionHandler
{
    public interface ICustomExceptionHandler
    {
        Task<CommonResponse<T>> HandleAsync<T>(Exception ex, string context, CommonResponse<T>? response = null);
    }
}

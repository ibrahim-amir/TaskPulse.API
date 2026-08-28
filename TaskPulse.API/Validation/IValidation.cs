using TaskPulse.API.CommonResponseClass;

namespace TaskPulse.API.Validation
{
    public interface IValidation<T>
    {
        Task<CommonResponse<T>> Validate(T obj);
    }
}

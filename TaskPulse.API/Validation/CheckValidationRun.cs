using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;

namespace TaskPulse.API.Validation
{
    public class CheckValidationRun
    {
        private readonly Dictionary<Type, object> _validators;

        // Add one line per new DTO that needs validation:
        // { typeof(CreateProjectDto), projectValidation },
        public CheckValidationRun(IValidation<CreateUserDto> userValidation)
        {
            _validators = new Dictionary<Type, object>
            {
                { typeof(CreateUserDto), userValidation }
            };
        }

        public async Task<CommonResponse<T>> Run<T>(T obj)
        {
            if (!_validators.TryGetValue(typeof(T), out var validatorObj))
                throw new NotSupportedException($"No validator registered for type '{typeof(T).Name}'.");

            var validator = (IValidation<T>)validatorObj;
            return await validator.Validate(obj);
        }
    }
}

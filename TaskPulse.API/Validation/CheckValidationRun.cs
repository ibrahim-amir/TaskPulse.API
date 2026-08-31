using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;

namespace TaskPulse.API.Validation
{
    public class CheckValidationRun
    {
        private readonly Dictionary<Type, object> _validators;

        // Add one line per new DTO that needs validation:
        // { typeof(CreateProjectDto), projectValidation },
        public CheckValidationRun(
            IValidation<CreateUserDto> userValidation,
            IValidation<LoginDto> loginValidation,
            IValidation<RefreshTokenRequestDto> refreshTokenValidation,
            IValidation<ConfirmEmailDto> confirmEmailValidation,
            IValidation<ResendConfirmationDto> resendConfirmationValidation,
            IValidation<ForgotPasswordDto> forgotPasswordValidation,
            IValidation<ResetPasswordDto> resetPasswordValidation,
            IValidation<UpdateUserProfileDto> updateUserProfileValidation,
            IValidation<UpdateUserRoleDto> updateUserRoleValidation,
            IValidation<UpdateUserStatusDto> updateUserStatusValidation)
        {
            _validators = new Dictionary<Type, object>
            {
                { typeof(CreateUserDto), userValidation },
                { typeof(LoginDto), loginValidation },
                { typeof(RefreshTokenRequestDto), refreshTokenValidation },
                { typeof(ConfirmEmailDto), confirmEmailValidation },
                { typeof(ResendConfirmationDto), resendConfirmationValidation },
                { typeof(ForgotPasswordDto), forgotPasswordValidation },
                { typeof(ResetPasswordDto), resetPasswordValidation },
                { typeof(UpdateUserProfileDto), updateUserProfileValidation },
                { typeof(UpdateUserRoleDto), updateUserRoleValidation },
                { typeof(UpdateUserStatusDto), updateUserStatusValidation }
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

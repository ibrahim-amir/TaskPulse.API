using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Validation.InputValidation.ForgotPasswordInputValidation;

namespace TaskPulse.API.Validation.UserValidation
{
    public class ForgotPasswordValidation : IValidation<ForgotPasswordDto>
    {
        private readonly ForgotPasswordInputValidation _inputValidation;

        public ForgotPasswordValidation(ForgotPasswordInputValidation inputValidation)
        {
            _inputValidation = inputValidation;
        }

        public async Task<CommonResponse<ForgotPasswordDto>> Validate(ForgotPasswordDto obj)
        {
            // Checking Input Validation.
            var inputValidation = _inputValidation.Validate(obj);
            var errors = new List<string>();

            if (!inputValidation.IsValid)
            {
                errors.AddRange(inputValidation.Errors.Select(e => e.ErrorMessage));
                return CommonResponseFactory.CreateErrorResponse<ForgotPasswordDto>(errors, HttpStatusCode.BadRequest);
            }

            // Same anti-enumeration reasoning as ResendConfirmationValidation —
            // no existence check, no different response for a real vs fake email.
            return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
        }
    }
}

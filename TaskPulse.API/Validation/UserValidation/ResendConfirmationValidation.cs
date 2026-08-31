using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Validation.InputValidation.ResendConfirmationInputValidation;

namespace TaskPulse.API.Validation.UserValidation
{
    public class ResendConfirmationValidation : IValidation<ResendConfirmationDto>
    {
        private readonly ResendConfirmationInputValidation _inputValidation;

        public ResendConfirmationValidation(ResendConfirmationInputValidation inputValidation)
        {
            _inputValidation = inputValidation;
        }

        public Task<CommonResponse<ResendConfirmationDto>> Validate(ResendConfirmationDto obj)
        {
            var inputValidation = _inputValidation.Validate(obj);
            var errors = new List<string>();

            if (!inputValidation.IsValid)
            {
                errors.AddRange(inputValidation.Errors.Select(e => e.ErrorMessage));
                return Task.FromResult(CommonResponseFactory.CreateErrorResponse<ResendConfirmationDto>(errors, HttpStatusCode.BadRequest));
            }

            // Deliberately no existence check here — the service returns the same
            // generic message whether or not the email is registered, so this
            // endpoint can't be used to enumerate accounts.
            return Task.FromResult(CommonResponseFactory.CreateSuccessResponse(obj, "Valid."));
        }
    }
}

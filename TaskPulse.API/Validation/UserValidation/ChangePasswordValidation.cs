using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Helper.PasswordHelperClass;
using TaskPulse.API.Repository.UserRepo;
using TaskPulse.API.Validation.InputValidation.ChangePasswordInputValidation;

namespace TaskPulse.API.Validation.UserValidation
{
    public class ChangePasswordValidation : IValidation<ChangePasswordDto>
    {
        private readonly ChangePasswordInputValidation _inputValidation;
        private readonly UserRepository _userRepository;

        public ChangePasswordValidation(ChangePasswordInputValidation inputValidation, UserRepository userRepository)
        {
            _inputValidation = inputValidation;
            _userRepository = userRepository;
        }

        public async Task<CommonResponse<ChangePasswordDto>> Validate(ChangePasswordDto obj)
        {
            var inputValidation = _inputValidation.Validate(obj);
            var errors = new List<string>();
            if (!inputValidation.IsValid)
            {
                errors.AddRange(inputValidation.Errors.Select(e => e.ErrorMessage));
                return CommonResponseFactory.CreateErrorResponse<ChangePasswordDto>(errors, HttpStatusCode.BadRequest);
            }

            var user = await _userRepository.GetByIdAsync(obj.Id);
            if (user is null)
                return CommonResponseFactory.CreateErrorResponse<ChangePasswordDto>("User not found.", HttpStatusCode.NotFound);

            if (!PasswordHelper.VerifyPassword(obj.CurrentPassword, user.PasswordHash))
            {
                errors.Add("Current password is incorrect.");
                return CommonResponseFactory.CreateErrorResponse<ChangePasswordDto>(errors, HttpStatusCode.BadRequest);
            }

            return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
        }
    }
}

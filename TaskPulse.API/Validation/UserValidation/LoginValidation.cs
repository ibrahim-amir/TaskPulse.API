using System.Net;
using TaskPulse.API.Authentication;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Helper.EntityFieldNameEnum;
using TaskPulse.API.Helper.PasswordHelperClass;
using TaskPulse.API.Helper.ValidationHelper.EntityExistenceValidationHelper;
using TaskPulse.API.Repository.UserRepo;
using TaskPulse.API.Validation.InputValidation.LoginInputValidation;

namespace TaskPulse.API.Validation.UserValidation
{
    public class LoginValidation : IValidation<LoginDto>
    {
        private readonly LoginUserInputValidation _inputValidation;
        private readonly UserRepository _userRepository;
        private readonly EntityExistenceChecker _existenceChecker;

        public LoginValidation(LoginUserInputValidation validation, UserRepository userRepository, EntityExistenceChecker existenceChecker)
        {
            _inputValidation = validation;
            _userRepository = userRepository;
            _existenceChecker = existenceChecker;
        }

        public async Task<CommonResponse<LoginDto>> Validate(LoginDto obj)
        {
            var inputValidation = _inputValidation.Validate(obj);
            var errors = new List<string>();
            //Check input validation.

            if (!inputValidation.IsValid)
            {
                errors.AddRange(inputValidation.Errors.Select(e => e.ErrorMessage));
                return CommonResponseFactory.CreateErrorResponse<LoginDto>(errors, HttpStatusCode.BadRequest);
            }

            //Check the email actually exists before we touch the password at all.
            var missing = await _existenceChecker.CheckAllFieldExists((typeof(User), Entity.User.Email.ToText(), obj.Email));
            if (missing.Count > 0)
            {
                errors.Add("Invalid email or password.");
                return CommonResponseFactory.CreateErrorResponse<LoginDto>(errors, HttpStatusCode.BadRequest);
            }

            //Check Password Validation.
            var user = await _userRepository.GetByEmailAsync(obj.Email);
            if (user is null || !PasswordHelper.VerifyPassword(obj.Password, user.PasswordHash))
            {
                errors.Add("Invalid email or password.");
                return CommonResponseFactory.CreateErrorResponse<LoginDto>(errors, HttpStatusCode.BadRequest);
            }

            //If validated.
            return CommonResponseFactory.CreateSuccessResponse<LoginDto>(obj, "valid");

        }
    }
}

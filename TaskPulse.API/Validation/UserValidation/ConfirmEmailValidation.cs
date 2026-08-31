using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Helper.EntityFieldNameEnum;
using TaskPulse.API.Helper.TokenHelperClass;
using TaskPulse.API.Helper.ValidationHelper.EntityExistenceValidationHelper;
using TaskPulse.API.Repository.UserRepo;
using TaskPulse.API.Validation.InputValidation.ConfirmEmailInputValidation;

namespace TaskPulse.API.Validation.UserValidation
{
    public class ConfirmEmailValidation : IValidation<ConfirmEmailDto>
    {
        private readonly ConfirmEmailInputValidation _inputValidation;
        private readonly UserRepository _userRepository;
        private readonly EntityExistenceChecker _existenceChecker;

        public ConfirmEmailValidation(ConfirmEmailInputValidation inputValidation, UserRepository userRepository, EntityExistenceChecker existenceChecker)
        {
            _inputValidation = inputValidation;
            _userRepository = userRepository;
            _existenceChecker = existenceChecker;
        }

        public async Task<CommonResponse<ConfirmEmailDto>> Validate(ConfirmEmailDto obj)
        {
            var inputValidation = _inputValidation.Validate(obj);
            var errors = new List<string>();

            //Checking input validation.
            if (!inputValidation.IsValid)
            {
                errors.AddRange(inputValidation.Errors.Select(e => e.ErrorMessage));
                return CommonResponseFactory.CreateErrorResponse<ConfirmEmailDto>(errors, HttpStatusCode.BadRequest);
            }


            var missing = await _existenceChecker.CheckAllFieldExists  //checking if the data already exists.
            (
                (typeof(User), Entity.User.Email.ToText(), obj.Email)
            );

            if (missing.Count > 0)
            {
                errors.Add("Invalid email or confirmation token.");
                return CommonResponseFactory.CreateErrorResponse<ConfirmEmailDto>(errors, HttpStatusCode.BadRequest);
            }

            // Get Email and token.
            var user = await _userRepository.GetByEmailAsync(obj.Email);
            var tokenHash = TokenHasher.HashToken(obj.Token);


            // Base Check.
            if (user is null || user.EmailConfirmationTokenHash != tokenHash || user.EmailConfirmationTokenExpiresAt is null || user.EmailConfirmationTokenExpiresAt < DateTime.UtcNow)
            {
                errors.Add("Invalid email or confirmation token.");
                return CommonResponseFactory.CreateErrorResponse<ConfirmEmailDto>(errors, HttpStatusCode.BadRequest);
            }

            //Response.
            return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
        }
    }
}

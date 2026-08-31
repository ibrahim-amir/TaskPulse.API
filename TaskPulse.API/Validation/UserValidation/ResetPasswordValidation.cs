using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Helper.TokenHelperClass;
using TaskPulse.API.Repository.UserRepo;
using TaskPulse.API.Validation.InputValidation.ResetPasswordInputValidation;

namespace TaskPulse.API.Validation.UserValidation
{
    public class ResetPasswordValidation : IValidation<ResetPasswordDto>
    {
        private readonly ResetPasswordInputValidation _inputValidation;
        private readonly UserRepository _userRepository;

        public ResetPasswordValidation(ResetPasswordInputValidation inputValidation, UserRepository userRepository)
        {
            _inputValidation = inputValidation;
            _userRepository = userRepository;
        }

        public async Task<CommonResponse<ResetPasswordDto>> Validate(ResetPasswordDto obj)
        {

            // Input Validation.
            var inputValidation = _inputValidation.Validate(obj);
            var errors = new List<string>();
            if (!inputValidation.IsValid)
            {
                errors.AddRange(inputValidation.Errors.Select(e => e.ErrorMessage));
                return CommonResponseFactory.CreateErrorResponse<ResetPasswordDto>(errors, HttpStatusCode.BadRequest);
            }

            // Get Token Hash
            var user = await _userRepository.GetByEmailAsync(obj.Email);
            var tokenHash = TokenHasher.HashToken(obj.Token);


            if (user is null || user.PasswordResetTokenHash != tokenHash
                || user.PasswordResetTokenExpiresAt is null || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            {
                errors.Add("Invalid or expired reset link.");
                return CommonResponseFactory.CreateErrorResponse<ResetPasswordDto>(errors, HttpStatusCode.BadRequest);
            }

            return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
        }
    }
}

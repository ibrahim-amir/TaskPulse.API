using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Helper.TokenHelperClass;
using TaskPulse.API.Repository.UserRepo;
using TaskPulse.API.Validation.InputValidation.RefreshTokenInputValidation;

namespace TaskPulse.API.Validation.UserValidation
{
    public class RefreshTokenValidation : IValidation<RefreshTokenRequestDto>
    {
        private readonly RefreshTokenInputValidation _inputValidation;
        private readonly UserRepository _userRepository;

        public RefreshTokenValidation(RefreshTokenInputValidation inputValidation, UserRepository userRepository)
        {
            _inputValidation = inputValidation;
            _userRepository = userRepository;
        }

        public async Task<CommonResponse<RefreshTokenRequestDto>> Validate(RefreshTokenRequestDto obj)
        {
            //Input Validation Check.
            var inputValidation = _inputValidation.Validate(obj);
            var errors = new List<string>();
            if (!inputValidation.IsValid)
            {
                errors.AddRange(inputValidation.Errors.Select(e => e.ErrorMessage));
                return CommonResponseFactory.CreateErrorResponse<RefreshTokenRequestDto>(errors, HttpStatusCode.BadRequest);
            }


            // Look the token up by its hash
            var user = await _userRepository.GetByRefreshTokenHashAsync(TokenHasher.HashToken(obj.RefreshToken));

            if (user is null || user.RefreshTokenExpiresAt is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            {
                errors.Add("Refresh token is invalid or expired.");
                return CommonResponseFactory.CreateErrorResponse<RefreshTokenRequestDto>(errors, HttpStatusCode.Unauthorized);
            }

            return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
        }
    }
}

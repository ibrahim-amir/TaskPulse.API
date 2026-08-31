using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Repository.UserRepo;

namespace TaskPulse.API.Validation.UserValidation
{
    public class UpdateUserStatusValidation : IValidation<UpdateUserStatusDto>
    {
        private readonly UserRepository _userRepository;

        public UpdateUserStatusValidation(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<CommonResponse<UpdateUserStatusDto>> Validate(UpdateUserStatusDto obj)
        {
            var user = await _userRepository.GetByIdAsync(obj.Id);
            if (user is null)
                return CommonResponseFactory.CreateErrorResponse<UpdateUserStatusDto>("User not found.", HttpStatusCode.NotFound);

            //Same reasoning as the role check: disabling the last Admin locks
            //everyone out of account management permanently.
            if (obj.IsDisabled && user.Role == UserRole.Admin && await _userRepository.CountActiveAdminsAsync() <= 1)
                return CommonResponseFactory.CreateErrorResponse<UpdateUserStatusDto>(
                    "This is the last active Admin — promote another user to Admin first.",
                    HttpStatusCode.BadRequest);

            return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
        }
    }
}

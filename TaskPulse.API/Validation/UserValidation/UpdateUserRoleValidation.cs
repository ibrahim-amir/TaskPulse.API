using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Repository.UserRepo;

namespace TaskPulse.API.Validation.UserValidation
{
    public class UpdateUserRoleValidation : IValidation<UpdateUserRoleDto>
    {
        private readonly UserRepository _userRepository;

        public UpdateUserRoleValidation(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<CommonResponse<UpdateUserRoleDto>> Validate(UpdateUserRoleDto obj)
        {
            var user = await _userRepository.GetByIdAsync(obj.Id);
            if (user is null)
                return CommonResponseFactory.CreateErrorResponse<UpdateUserRoleDto>("User not found.", HttpStatusCode.NotFound);

            //Accounts can only be created by an Admin, so demoting the last one
            //would leave nobody able to create or manage users ever again.
            if (user.Role == UserRole.Admin && obj.Role != UserRole.Admin && await _userRepository.CountActiveAdminsAsync() <= 1)
                return CommonResponseFactory.CreateErrorResponse<UpdateUserRoleDto>(
                    "This is the last active Admin — promote another user to Admin first.",
                    HttpStatusCode.BadRequest);

            return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
        }
    }
}

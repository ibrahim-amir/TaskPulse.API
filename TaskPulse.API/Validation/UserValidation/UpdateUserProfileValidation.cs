using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Helper.EntityFieldNameEnum;
using TaskPulse.API.Helper.ValidationHelper.EntityExistenceValidationHelper;
using TaskPulse.API.Validation.InputValidation.UpdateUserInputValidation;

namespace TaskPulse.API.Validation.UserValidation
{
    public class UpdateUserProfileValidation : IValidation<UpdateUserProfileDto>
    {
        private readonly UpdateUserProfileInputValidation _inputValidation;
        private readonly EntityExistenceChecker _existenceChecker;

        public UpdateUserProfileValidation(UpdateUserProfileInputValidation inputValidation, EntityExistenceChecker existenceChecker)
        {
            _inputValidation = inputValidation;
            _existenceChecker = existenceChecker;
        }

        public async Task<CommonResponse<UpdateUserProfileDto>> Validate(UpdateUserProfileDto obj)
        {
            //Input Validation.
            var inputValidationResult = _inputValidation.Validate(obj);
            var errors = new List<string>();

            if (!inputValidationResult.IsValid)
            {
                errors.AddRange(inputValidationResult.Errors.Select(e => e.ErrorMessage));
                return CommonResponseFactory.CreateErrorResponse<UpdateUserProfileDto>(errors, HttpStatusCode.BadRequest);
            }

            //The user being updated has to exist.
            var missing = await _existenceChecker.CheckAllByID((typeof(User), obj.Id));
            if (missing.Count > 0)
                return CommonResponseFactory.CreateErrorResponse<UpdateUserProfileDto>("User not found.", HttpStatusCode.NotFound);

            //Name stays unique, but this user's own name doesn't count against them.
            errors.AddRange(await _existenceChecker.CheckFieldsAreUniqueExcluding
            (
                obj.Id,
                (typeof(User), Entity.User.Name.ToText(), obj.Name)
            ));

            if (errors.Count > 0)
                return CommonResponseFactory.CreateErrorResponse<UpdateUserProfileDto>(errors, HttpStatusCode.BadRequest);

            return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
        }
    }
}

using System.Net;
using TaskPulse.API.Validation;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Helper.ValidationHelper.EntityExistenceValidationHelper;
using TaskPulse.API.Entities;
using TaskPulse.API.Helper.EntityFieldNameEnum;
using TaskPulse.API.Validation.InputValidation.UserInputValidation;

public class CreateUserValidation : IValidation<CreateUserDto>
{
    private readonly EntityExistenceChecker _existance;
    private readonly CreateUserInputValidation _userInputValidation;
    public CreateUserValidation(EntityExistenceChecker existance, CreateUserInputValidation userInputValidation)
    {
        _existance = existance;
        _userInputValidation = userInputValidation;
    }

    public async Task<CommonResponse<CreateUserDto>> Validate(CreateUserDto obj)
    {
        //Input Validation.
        var inputValidationResult = _userInputValidation.Validate(obj);
        var errors = new List<string>();

        if (!inputValidationResult.IsValid)
        {
            errors.AddRange(inputValidationResult.Errors.Select(e => e.ErrorMessage));
            return CommonResponseFactory.CreateErrorResponse<CreateUserDto>(errors, HttpStatusCode.BadRequest);
        }

        //Existance validation.
        errors.AddRange(await _existance.CheckFieldsAreUnique
        (
            (typeof(User), Entity.User.Name.ToText(), obj.Name),
            (typeof(User), Entity.User.Email.ToText(), obj.Email)
        ));

        //Checking fields.
        if (errors.Count > 0)
            return CommonResponseFactory.CreateErrorResponse<CreateUserDto>(errors, HttpStatusCode.BadRequest);

        return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
    }
}

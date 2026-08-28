using System.Net;
using TaskPulse.API.Validation;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Helper.ValidationHelper.EmailValidationHelper;
using TaskPulse.API.Helper.ValidationHelper.PasswordValidationHelper;
using TaskPulse.API.Helper.ValidationHelper.EntityExistenceValidationHelper;
using TaskPulse.API.Entities;
using TaskPulse.API.Helper.EntityFieldNameEnum;


public class CreateUserValidation : IValidation<CreateUserDto>
{
    private readonly EntityExistenceChecker _existance;
    public CreateUserValidation(EntityExistenceChecker existance)
    {
        _existance = existance;
    }


    public async Task<CommonResponse<CreateUserDto>> Validate(CreateUserDto obj)
    {
        var errors = new List<string>();


        if (string.IsNullOrWhiteSpace(obj.Name)) errors.Add("Name is required.");
        if (string.IsNullOrWhiteSpace(obj.Email)) errors.Add("Email is required.");
        if (string.IsNullOrWhiteSpace(obj.Password)) errors.Add("Password is required.");
        if (!Enum.IsDefined(typeof(UserRole), obj.Role)) errors.Add($"'{obj.Role}' is not a valid role.");


        errors.AddRange(await _existance.CheckFieldsAreUnique(
            (typeof(User), Entity.User.Name.ToText(), obj.Name),
            (typeof(User), Entity.User.Email.ToText(), obj.Email)
            ));

        
        //Password strength validation
        if (!PasswordStrengthValidator.CheckStrength(obj.Password, out var passwordIssue))
            errors.Add(passwordIssue);

        if (!EmailValidator.CheckEmail(obj.Email, out var emailIssue))
            errors.Add(emailIssue);


        if (errors.Count > 0)
            return CommonResponseFactory.CreateErrorResponse<CreateUserDto>(errors, HttpStatusCode.BadRequest);

        return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
    }
}

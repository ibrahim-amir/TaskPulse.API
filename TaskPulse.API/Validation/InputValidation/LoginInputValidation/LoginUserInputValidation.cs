using FluentValidation;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Helper.ValidationHelper;

namespace TaskPulse.API.Validation.InputValidation.LoginInputValidation
{
    public class LoginUserInputValidation : AbstractValidator<LoginDto>
    {
        public LoginUserInputValidation()
        {
            RuleFor(x => x.Email)
           .NotEmpty().WithMessage("Email is required.")
           .IsValidEmail();

            RuleFor(x => x.Password)
           .NotEmpty().WithMessage("Password is required.");
        }
    }
}

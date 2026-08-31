using FluentValidation;
using TaskPulse.API.DTO.UserDto;

namespace TaskPulse.API.Validation.InputValidation.LoginInputValidation
{
    public class LoginUserInputValidation : AbstractValidator<LoginDto>
    {
        public LoginUserInputValidation()
        {
            RuleFor(x => x.Email)
           .NotEmpty().WithMessage("Email is required.")
           .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Password)
           .NotEmpty().WithMessage("Password is required.");
        }
    }
}

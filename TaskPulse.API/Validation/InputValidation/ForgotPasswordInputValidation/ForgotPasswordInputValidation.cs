using FluentValidation;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Helper.ValidationHelper;

namespace TaskPulse.API.Validation.InputValidation.ForgotPasswordInputValidation
{
    public class ForgotPasswordInputValidation : AbstractValidator<ForgotPasswordDto>
    {
        public ForgotPasswordInputValidation()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .IsValidEmail();
        }
    }
}

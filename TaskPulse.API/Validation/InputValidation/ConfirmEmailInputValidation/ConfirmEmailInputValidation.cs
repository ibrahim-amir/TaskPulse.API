using FluentValidation;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Helper.ValidationHelper;

namespace TaskPulse.API.Validation.InputValidation.ConfirmEmailInputValidation
{
    public class ConfirmEmailInputValidation : AbstractValidator<ConfirmEmailDto>
    {
        public ConfirmEmailInputValidation()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .IsValidEmail();

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Confirmation token is required.");
        }
    }
}

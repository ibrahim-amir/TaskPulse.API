using FluentValidation;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Helper.ValidationHelper;

namespace TaskPulse.API.Validation.InputValidation.ResendConfirmationInputValidation
{
    public class ResendConfirmationInputValidation : AbstractValidator<ResendConfirmationDto>
    {
        public ResendConfirmationInputValidation()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .IsValidEmail();
        }
    }
}

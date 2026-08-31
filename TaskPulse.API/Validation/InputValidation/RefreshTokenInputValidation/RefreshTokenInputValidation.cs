using FluentValidation;
using TaskPulse.API.DTO.UserDto;

namespace TaskPulse.API.Validation.InputValidation.RefreshTokenInputValidation
{
    public class RefreshTokenInputValidation : AbstractValidator<RefreshTokenRequestDto>
    {
        public RefreshTokenInputValidation()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required.");
        }
    }
}

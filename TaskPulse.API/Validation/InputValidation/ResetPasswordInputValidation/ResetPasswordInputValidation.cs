using FluentValidation;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Helper.ValidationHelper;

namespace TaskPulse.API.Validation.InputValidation.ResetPasswordInputValidation
{
    public class ResetPasswordInputValidation : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordInputValidation()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .IsValidEmail();

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Reset token is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one number.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("Confirm password is required.")
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
        }
    }
}

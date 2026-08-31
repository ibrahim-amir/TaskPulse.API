using FluentValidation;
using TaskPulse.API.DTO.UserDto;

namespace TaskPulse.API.Validation.InputValidation.UserInputValidation;
public class CreateUserInputValidation : AbstractValidator<CreateUserDto>
{
    public CreateUserInputValidation()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.confirmPassword)
            .NotEmpty().WithMessage("Confirm Password is required.")
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}
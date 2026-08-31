using FluentValidation;
using TaskPulse.API.DTO.UserDto;

namespace TaskPulse.API.Validation.InputValidation.UpdateUserInputValidation;

public class UpdateUserProfileInputValidation : AbstractValidator<UpdateUserProfileDto>
{
    public UpdateUserProfileInputValidation()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("A valid user id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.profileImageUrl)
            .MaximumLength(500).WithMessage("Profile image URL cannot exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.profileImageUrl));
    }
}

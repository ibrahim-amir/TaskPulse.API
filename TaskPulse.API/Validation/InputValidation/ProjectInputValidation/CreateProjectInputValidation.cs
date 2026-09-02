using FluentValidation;
using TaskPulse.API.DTO.ProjectDto;

namespace TaskPulse.API.Validation.InputValidation.ProjectInputValidation;

public class CreateProjectInputValidation : AbstractValidator<CreateProjectDto>
{
    public CreateProjectInputValidation()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(150).WithMessage("Project name cannot exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date cannot be before the start date.")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x.ManagerUserId)
            .GreaterThan(0).WithMessage("A valid manager id is required.")
            .When(x => x.ManagerUserId.HasValue);
    }
}

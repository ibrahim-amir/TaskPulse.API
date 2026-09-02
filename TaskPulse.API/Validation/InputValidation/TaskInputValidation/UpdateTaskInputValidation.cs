using FluentValidation;
using TaskPulse.API.DTO.TaskDto;

namespace TaskPulse.API.Validation.InputValidation.TaskInputValidation;

public class UpdateTaskInputValidation : AbstractValidator<UpdateTaskDto>
{
    public UpdateTaskInputValidation()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("A valid task id is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(200).WithMessage("Task title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Description cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.EstimatedHours)
            .GreaterThan(0).WithMessage("Estimated hours must be greater than zero.")
            .LessThanOrEqualTo(9999).WithMessage("Estimated hours is unrealistically large.")
            .When(x => x.EstimatedHours.HasValue);

        RuleFor(x => x.ActualHours)
            .GreaterThanOrEqualTo(0).WithMessage("Actual hours cannot be negative.")
            .LessThanOrEqualTo(9999).WithMessage("Actual hours is unrealistically large.")
            .When(x => x.ActualHours.HasValue);
    }
}

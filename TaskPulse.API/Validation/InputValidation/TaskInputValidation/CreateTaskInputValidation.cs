using FluentValidation;
using TaskPulse.API.DTO.TaskDto;

namespace TaskPulse.API.Validation.InputValidation.TaskInputValidation;

public class CreateTaskInputValidation : AbstractValidator<CreateTaskDto>
{
    public CreateTaskInputValidation()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("A valid project id is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(200).WithMessage("Task title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Description cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(_ => DateTime.UtcNow.Date).WithMessage("Due date cannot be in the past.")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.EstimatedHours)
            .GreaterThan(0).WithMessage("Estimated hours must be greater than zero.")
            .LessThanOrEqualTo(9999).WithMessage("Estimated hours is unrealistically large.")
            .When(x => x.EstimatedHours.HasValue);

        RuleForEach(x => x.SubtaskLabels)
            .NotEmpty().WithMessage("A required file needs a label.")
            .MaximumLength(200).WithMessage("A required file label cannot exceed 200 characters.");
    }
}

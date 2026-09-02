using FluentValidation;
using TaskPulse.API.DTO.ProjectMemberDto;

namespace TaskPulse.API.Validation.InputValidation.ProjectInputValidation;

public class AddProjectMemberInputValidation : AbstractValidator<AddProjectMemberDto>
{
    public AddProjectMemberInputValidation()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("A valid project id is required.");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("A valid user id is required.");
    }
}

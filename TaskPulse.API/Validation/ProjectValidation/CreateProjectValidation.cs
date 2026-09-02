using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.ProjectDto;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Validation.InputValidation.ProjectInputValidation;

namespace TaskPulse.API.Validation.ProjectValidation
{
    public class CreateProjectValidation : IValidation<CreateProjectDto>
    {
        private readonly CreateProjectInputValidation _inputValidation;
        private readonly ProjectRepository _projectRepository;

        public CreateProjectValidation(CreateProjectInputValidation inputValidation, ProjectRepository projectRepository)
        {
            _inputValidation = inputValidation;
            _projectRepository = projectRepository;
        }

        public async Task<CommonResponse<CreateProjectDto>> Validate(CreateProjectDto obj)
        {
            //Input Validation.
            var inputValidationResult = _inputValidation.Validate(obj);
            var errors = new List<string>();

            if (!inputValidationResult.IsValid)
            {
                errors.AddRange(inputValidationResult.Errors.Select(e => e.ErrorMessage));
                return CommonResponseFactory.CreateErrorResponse<CreateProjectDto>(errors, HttpStatusCode.BadRequest);
            }

            //Project names stay unique so they're unambiguous in lists and reports.
            if (await _projectRepository.NameExistsAsync(obj.Name))
                return CommonResponseFactory.CreateErrorResponse<CreateProjectDto>("A project with that name already exists.", HttpStatusCode.BadRequest);

            return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
        }
    }
}

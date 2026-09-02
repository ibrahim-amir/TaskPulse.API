using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.ProjectDto;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Validation.InputValidation.ProjectInputValidation;

namespace TaskPulse.API.Validation.ProjectValidation
{
    public class UpdateProjectValidation : IValidation<UpdateProjectDto>
    {
        private readonly UpdateProjectInputValidation _inputValidation;
        private readonly ProjectRepository _projectRepository;

        public UpdateProjectValidation(UpdateProjectInputValidation inputValidation, ProjectRepository projectRepository)
        {
            _inputValidation = inputValidation;
            _projectRepository = projectRepository;
        }

        public async Task<CommonResponse<UpdateProjectDto>> Validate(UpdateProjectDto obj)
        {
            //Input Validation.
            var inputValidationResult = _inputValidation.Validate(obj);
            var errors = new List<string>();

            if (!inputValidationResult.IsValid)
            {
                errors.AddRange(inputValidationResult.Errors.Select(e => e.ErrorMessage));
                return CommonResponseFactory.CreateErrorResponse<UpdateProjectDto>(errors, HttpStatusCode.BadRequest);
            }

            //Unique against every other project, but not against itself.
            if (await _projectRepository.NameExistsAsync(obj.Name, obj.Id))
                return CommonResponseFactory.CreateErrorResponse<UpdateProjectDto>("A project with that name already exists.", HttpStatusCode.BadRequest);

            return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
        }
    }
}

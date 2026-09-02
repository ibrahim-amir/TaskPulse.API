using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Helper.ValidationHelper.EntityExistenceValidationHelper;
using TaskPulse.API.Validation.InputValidation.TaskInputValidation;

namespace TaskPulse.API.Validation.TaskValidation
{
    public class CreateTaskValidation : IValidation<CreateTaskDto>
    {
        private readonly CreateTaskInputValidation _inputValidation;
        private readonly EntityExistenceChecker _existenceChecker;

        public CreateTaskValidation(CreateTaskInputValidation inputValidation, EntityExistenceChecker existenceChecker)
        {
            _inputValidation = inputValidation;
            _existenceChecker = existenceChecker;
        }

        public async Task<CommonResponse<CreateTaskDto>> Validate(CreateTaskDto obj)
        {
            //Input Validation.
            var inputValidationResult = _inputValidation.Validate(obj);
            var errors = new List<string>();

            if (!inputValidationResult.IsValid)
            {
                errors.AddRange(inputValidationResult.Errors.Select(e => e.ErrorMessage));
                return CommonResponseFactory.CreateErrorResponse<CreateTaskDto>(errors, HttpStatusCode.BadRequest);
            }

            //The project has to exist. Whether the assignees are on it is a
            //membership question, so the service answers that one.
            errors.AddRange(await _existenceChecker.CheckAllByID((typeof(Project), obj.ProjectId)));

            foreach (var userId in obj.AssignedUserIds.Distinct())
                errors.AddRange(await _existenceChecker.CheckAllByID((typeof(User), userId)));

            if (errors.Count > 0)
                return CommonResponseFactory.CreateErrorResponse<CreateTaskDto>(errors, HttpStatusCode.BadRequest);

            return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
        }
    }
}

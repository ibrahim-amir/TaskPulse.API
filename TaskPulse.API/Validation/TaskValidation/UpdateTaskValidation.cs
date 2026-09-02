using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.Validation.InputValidation.TaskInputValidation;

namespace TaskPulse.API.Validation.TaskValidation
{
    public class UpdateTaskValidation : IValidation<UpdateTaskDto>
    {
        private readonly UpdateTaskInputValidation _inputValidation;

        public UpdateTaskValidation(UpdateTaskInputValidation inputValidation)
        {
            _inputValidation = inputValidation;
        }

        public Task<CommonResponse<UpdateTaskDto>> Validate(UpdateTaskDto obj)
        {
            //Input Validation. The task's existence and who may edit it are the
            //service's job, since both depend on the caller.
            var inputValidationResult = _inputValidation.Validate(obj);

            if (!inputValidationResult.IsValid)
            {
                var errors = inputValidationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Task.FromResult(CommonResponseFactory.CreateErrorResponse<UpdateTaskDto>(errors, HttpStatusCode.BadRequest));
            }

            return Task.FromResult(CommonResponseFactory.CreateSuccessResponse(obj, "Valid."));
        }
    }
}

using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.ProjectMemberDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Helper.ValidationHelper.EntityExistenceValidationHelper;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Repository.UserRepo;
using TaskPulse.API.Validation.InputValidation.ProjectInputValidation;

namespace TaskPulse.API.Validation.ProjectValidation
{
    public class AddProjectMemberValidation : IValidation<AddProjectMemberDto>
    {
        private readonly AddProjectMemberInputValidation _inputValidation;
        private readonly EntityExistenceChecker _existenceChecker;
        private readonly ProjectRepository _projectRepository;
        private readonly UserRepository _userRepository;

        public AddProjectMemberValidation(
            AddProjectMemberInputValidation inputValidation,
            EntityExistenceChecker existenceChecker,
            ProjectRepository projectRepository,
            UserRepository userRepository)
        {
            _inputValidation = inputValidation;
            _existenceChecker = existenceChecker;
            _projectRepository = projectRepository;
            _userRepository = userRepository;
        }

        public async Task<CommonResponse<AddProjectMemberDto>> Validate(AddProjectMemberDto obj)
        {
            //Input Validation.
            var inputValidationResult = _inputValidation.Validate(obj);
            var errors = new List<string>();

            if (!inputValidationResult.IsValid)
            {
                errors.AddRange(inputValidationResult.Errors.Select(e => e.ErrorMessage));
                return CommonResponseFactory.CreateErrorResponse<AddProjectMemberDto>(errors, HttpStatusCode.BadRequest);
            }

            //Both the project and the user have to exist.
            errors.AddRange(await _existenceChecker.CheckAllByID
            (
                (typeof(Project), obj.ProjectId),
                (typeof(User), obj.UserId)
            ));

            if (errors.Count > 0)
                return CommonResponseFactory.CreateErrorResponse<AddProjectMemberDto>(errors, HttpStatusCode.BadRequest);

            //A disabled account shouldn't be picking up new project work.
            var user = await _userRepository.GetByIdAsync(obj.UserId);
            if (user is not null && user.IsDisabled)
                return CommonResponseFactory.CreateErrorResponse<AddProjectMemberDto>("That account is disabled.", HttpStatusCode.BadRequest);

            //There's a unique index on (ProjectId, UserId); catch it here so it
            //comes back as a readable message instead of a database error.
            if (await _projectRepository.IsMemberAsync(obj.ProjectId, obj.UserId))
                return CommonResponseFactory.CreateErrorResponse<AddProjectMemberDto>("That user is already on this project.", HttpStatusCode.BadRequest);

            return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
        }
    }
}

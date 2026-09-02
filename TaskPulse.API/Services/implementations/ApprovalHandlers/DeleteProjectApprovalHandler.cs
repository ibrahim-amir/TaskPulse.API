using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.Exceptions;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations.ApprovalHandlers
{
    public class DeleteProjectApprovalHandler : IApprovalActionHandler
    {
        private readonly ProjectRepository _projectRepository;

        public DeleteProjectApprovalHandler(ProjectRepository projectRepository)
        {
            _projectRepository = projectRepository;
        }

        public ApprovalActionType ActionType => ApprovalActionType.DeleteProject;

        public async Task ExecuteAsync(ApprovalRequest request)
        {
            var project = await _projectRepository.GetByIdAsync(request.TargetEntityId);

            // The project can have been deleted some other way while the
            // request sat waiting — that's a stale request, not a crash.
            if (project is null)
                throw new DataNotFound($"Project #{request.TargetEntityId} no longer exists.");

            await _projectRepository.DeleteAsync(project);
        }
    }
}

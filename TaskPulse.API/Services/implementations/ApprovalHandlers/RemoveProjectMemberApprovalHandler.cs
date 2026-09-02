using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.Exceptions;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations.ApprovalHandlers
{
    public class RemoveProjectMemberApprovalHandler : IApprovalActionHandler
    {
        private readonly ProjectRepository _projectRepository;

        public RemoveProjectMemberApprovalHandler(ProjectRepository projectRepository)
        {
            _projectRepository = projectRepository;
        }

        public ApprovalActionType ActionType => ApprovalActionType.RemoveProjectMember;

        public async Task ExecuteAsync(ApprovalRequest request)
        {
            // The target here is the ProjectMember row itself, so the project
            // and user are both already pinned down by that one id.
            var member = await _projectRepository.GetMemberByIdAsync(request.TargetEntityId);
            if (member is null)
                throw new DataNotFound($"Project member #{request.TargetEntityId} no longer exists.");

            await _projectRepository.RemoveMemberAsync(member);
        }
    }
}

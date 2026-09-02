using System.Text.Json;
using TaskPulse.API.DTO.ProjectDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.Exceptions;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations.ApprovalHandlers
{
    public class UpdateProjectApprovalHandler : IApprovalActionHandler
    {
        private readonly ProjectRepository _projectRepository;

        public UpdateProjectApprovalHandler(ProjectRepository projectRepository)
        {
            _projectRepository = projectRepository;
        }

        public ApprovalActionType ActionType => ApprovalActionType.UpdateProject;

        public async Task ExecuteAsync(ApprovalRequest request)
        {
            // The edit the Manager asked for was serialised when the request was
            // filed; this replays it now that the Admin has said yes.
            if (string.IsNullOrWhiteSpace(request.PayloadJson))
                throw new DataNotFound($"Approval #{request.Id} has no change to apply.");

            var dto = JsonSerializer.Deserialize<UpdateProjectDto>(request.PayloadJson)
                      ?? throw new DataNotFound($"Approval #{request.Id} has an unreadable change payload.");

            var project = await _projectRepository.GetByIdAsync(request.TargetEntityId);
            if (project is null)
                throw new DataNotFound($"Project #{request.TargetEntityId} no longer exists.");

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.Status = dto.Status;
            project.Priority = dto.Priority;
            project.StartDate = dto.StartDate;
            project.EndDate = dto.EndDate;

            await _projectRepository.UpdateAsync(project);
        }
    }
}

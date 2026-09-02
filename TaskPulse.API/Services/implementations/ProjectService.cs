using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.ProjectDto;
using TaskPulse.API.DTO.ProjectMemberDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Repository.UserRepo;
using TaskPulse.API.Services.Infrastructure;
using TaskPulse.API.Services.interfaces;
using TaskPulse.API.Validation;

namespace TaskPulse.API.Services.implementations
{
    public class ProjectService : BaseServiceClass, IProjectService
    {
        private readonly CheckValidationRun _validationRunner;
        private readonly ProjectRepository _projectRepository;
        private readonly UserRepository _userRepository;
        private readonly IApprovalGate _approvalGate;
        private readonly IActivityLogger _activityLogger;
        private readonly INotifier _notifier;

        public ProjectService(
            CustomExceptionHandler customExceptionHandler,
            CheckValidationRun validationRunner,
            ProjectRepository projectRepository,
            UserRepository userRepository,
            IApprovalGate approvalGate,
            IActivityLogger activityLogger,
            INotifier notifier)
            : base(customExceptionHandler)
        {
            _validationRunner = validationRunner;
            _projectRepository = projectRepository;
            _userRepository = userRepository;
            _approvalGate = approvalGate;
            _activityLogger = activityLogger;
            _notifier = notifier;
        }


        // Get Projects Service Method — Admin sees everything, everyone else
        // only the projects they're a member of.
        public async Task<CommonResponse<PagedResult<ProjectDTO>>> GetProjectsAsync(ProjectQueryDto query, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<PagedResult<ProjectDTO>>();

            try
            {
                var page = await _projectRepository.GetPagedAsync(query, callerIsAdmin ? null : callerId);
                return CommonResponseFactory.CreateSuccessResponse(page.MapItems(ProjectDTO.FromEntity), "Projects retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Get Project By Id Service Method
        public async Task<CommonResponse<ProjectDTO>> GetProjectByIdAsync(int id, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<ProjectDTO>();

            try
            {
                var project = await _projectRepository.GetByIdAsync(id);
                if (project is null)
                    return CommonResponseFactory.CreateErrorResponse<ProjectDTO>("Project not found.", HttpStatusCode.NotFound);

                var memberAccess = ProjectAccess.CheckMember(project, callerId, callerIsAdmin);
                if (memberAccess is not null)
                    return CommonResponseFactory.CreateErrorResponse<ProjectDTO>(memberAccess, HttpStatusCode.Forbidden);

                return CommonResponseFactory.CreateSuccessResponse(ProjectDTO.FromEntity(project), "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Create Project Service Method — the creator's role is stamped onto the
        // project, since that's what decides who can change it later.
        public async Task<CommonResponse<ProjectDTO>> CreateProjectAsync(CreateProjectDto dto, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<ProjectDTO>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<ProjectDTO>(validation.Errors, validation.StatusCode);

                // The Admin doesn't run projects, so they have to name a Manager.
                // A Manager creating their own project runs it themselves.
                var managerUserId = dto.ManagerUserId ?? callerId;
                if (callerIsAdmin && dto.ManagerUserId is null)
                    return CommonResponseFactory.CreateErrorResponse<ProjectDTO>("Name the Manager who will run this project.", HttpStatusCode.BadRequest);

                var manager = await _userRepository.GetByIdAsync(managerUserId);
                if (manager is null)
                    return CommonResponseFactory.CreateErrorResponse<ProjectDTO>("The named manager does not exist.", HttpStatusCode.BadRequest);

                var project = new Project
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Status = dto.Status,
                    Priority = dto.Priority,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    CreatedByUserId = callerId,
                    CreatedByRole = callerIsAdmin ? UserRole.Admin : UserRole.Manager
                };

                await _projectRepository.CreateAsync(project);

                // The manager joins as a member, stamped with who put them there
                // so an Admin-placed manager can't be quietly removed later.
                await _projectRepository.AddMemberAsync(new ProjectMember
                {
                    ProjectId = project.Id,
                    UserId = manager.Id,
                    Role = UserRole.Manager,
                    AssignedByRole = project.CreatedByRole
                });

                await _activityLogger.LogAsync(callerId, ActivityAction.ProjectCreated, $"Created project '{project.Name}'.", project.Id);

                if (manager.Id != callerId)
                    await _notifier.NotifyAsync(manager.Id, NotificationType.AddedToProject,
                        $"You were made manager of '{project.Name}'.", nameof(Project), project.Id);

                var saved = await _projectRepository.GetByIdAsync(project.Id);
                return CommonResponseFactory.CreateSuccessResponse(ProjectDTO.FromEntity(saved!), CommonResponseMessage.Created.ToText());
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Update Project Service Method — goes through the approval gate when a
        // Manager is editing something the Admin created.
        public async Task<CommonResponse<ProjectDTO>> UpdateProjectAsync(UpdateProjectDto dto, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<ProjectDTO>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<ProjectDTO>(validation.Errors, validation.StatusCode);

                var project = await _projectRepository.GetByIdAsync(dto.Id);
                if (project is null)
                    return CommonResponseFactory.CreateErrorResponse<ProjectDTO>("Project not found.", HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckManager(project, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<ProjectDTO>(access, HttpStatusCode.Forbidden);

                var gate = await _approvalGate.ExecuteOrRequestApprovalAsync(
                    ApprovalActionType.UpdateProject,
                    nameof(Project),
                    project.Id,
                    NeedsApproval(project, callerIsAdmin),
                    callerId,
                    () => ApplyProjectChangesAsync(project, dto),
                    dto);

                if (!gate.Executed)
                    return PendingApprovalResponse<ProjectDTO>(gate);

                await _activityLogger.LogAsync(callerId, ActivityAction.ProjectUpdated, $"Updated project '{project.Name}'.", project.Id);

                return CommonResponseFactory.CreateSuccessResponse(ProjectDTO.FromEntity(project), "Project updated.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Delete Project Service Method — same gate as update.
        public async Task<CommonResponse<string>> DeleteProjectAsync(int id, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<string>();

            try
            {
                var project = await _projectRepository.GetByIdAsync(id);
                if (project is null)
                    return CommonResponseFactory.CreateErrorResponse<string>("Project not found.", HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckManager(project, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<string>(access, HttpStatusCode.Forbidden);

                var projectName = project.Name;

                var gate = await _approvalGate.ExecuteOrRequestApprovalAsync(
                    ApprovalActionType.DeleteProject,
                    nameof(Project),
                    project.Id,
                    NeedsApproval(project, callerIsAdmin),
                    callerId,
                    () => _projectRepository.DeleteAsync(project));

                if (!gate.Executed)
                    return PendingApprovalResponse<string>(gate);

                await _activityLogger.LogAsync(callerId, ActivityAction.ProjectDeleted, $"Deleted project '{projectName}'.");

                return CommonResponseFactory.CreateSuccessResponse("Project deleted.", "Project deleted.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Get Members Service Method
        public async Task<CommonResponse<List<ProjectMemberDTO>>> GetMembersAsync(int projectId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<List<ProjectMemberDTO>>();

            try
            {
                var project = await _projectRepository.GetByIdAsync(projectId);
                if (project is null)
                    return CommonResponseFactory.CreateErrorResponse<List<ProjectMemberDTO>>("Project not found.", HttpStatusCode.NotFound);

                var memberAccess = ProjectAccess.CheckMember(project, callerId, callerIsAdmin);
                if (memberAccess is not null)
                    return CommonResponseFactory.CreateErrorResponse<List<ProjectMemberDTO>>(memberAccess, HttpStatusCode.Forbidden);

                var members = await _projectRepository.GetMembersAsync(projectId);
                return CommonResponseFactory.CreateSuccessResponse(members.Select(ProjectMemberDTO.FromEntity).ToList(), "Members retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Add Member Service Method — adding isn't gated, only removing is.
        public async Task<CommonResponse<ProjectMemberDTO>> AddMemberAsync(AddProjectMemberDto dto, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<ProjectMemberDTO>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<ProjectMemberDTO>(validation.Errors, validation.StatusCode);

                var project = await _projectRepository.GetByIdAsync(dto.ProjectId);
                if (project is null)
                    return CommonResponseFactory.CreateErrorResponse<ProjectMemberDTO>("Project not found.", HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckManager(project, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<ProjectMemberDTO>(access, HttpStatusCode.Forbidden);

                var member = new ProjectMember
                {
                    ProjectId = dto.ProjectId,
                    UserId = dto.UserId,
                    Role = dto.Role,
                    AssignedByRole = callerIsAdmin ? UserRole.Admin : UserRole.Manager
                };

                await _projectRepository.AddMemberAsync(member);

                await _activityLogger.LogAsync(callerId, ActivityAction.ProjectMemberAdded, $"Added user #{dto.UserId} to '{project.Name}'.", project.Id);
                await _notifier.NotifyAsync(dto.UserId, NotificationType.AddedToProject,
                    $"You were added to '{project.Name}'.", nameof(Project), project.Id);

                var saved = await _projectRepository.GetMemberByIdAsync(member.Id);
                return CommonResponseFactory.CreateSuccessResponse(ProjectMemberDTO.FromEntity(saved!), CommonResponseMessage.Created.ToText());
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Remove Member Service Method — gated when the Admin is the one who
        // put that member on the project.
        public async Task<CommonResponse<string>> RemoveMemberAsync(int projectId, int userId, int callerId, bool callerIsAdmin)
        {
            var response = new CommonResponse<string>();

            try
            {
                var project = await _projectRepository.GetByIdAsync(projectId);
                if (project is null)
                    return CommonResponseFactory.CreateErrorResponse<string>("Project not found.", HttpStatusCode.NotFound);

                var access = ProjectAccess.CheckManager(project, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<string>(access, HttpStatusCode.Forbidden);

                var member = await _projectRepository.GetMemberAsync(projectId, userId);
                if (member is null)
                    return CommonResponseFactory.CreateErrorResponse<string>("That user is not a member of this project.", HttpStatusCode.NotFound);

                var needsApproval = !callerIsAdmin && member.AssignedByRole == UserRole.Admin;

                var gate = await _approvalGate.ExecuteOrRequestApprovalAsync(
                    ApprovalActionType.RemoveProjectMember,
                    nameof(ProjectMember),
                    member.Id,
                    needsApproval,
                    callerId,
                    () => _projectRepository.RemoveMemberAsync(member));

                if (!gate.Executed)
                    return PendingApprovalResponse<string>(gate);

                await _activityLogger.LogAsync(callerId, ActivityAction.ProjectMemberRemoved, $"Removed user #{userId} from '{project.Name}'.", project.Id);

                return CommonResponseFactory.CreateSuccessResponse("Member removed.", "Member removed.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Anything the Admin set up is off-limits to a Manager without a yes.
        private static bool NeedsApproval(Project project, bool callerIsAdmin)
        {
            return !callerIsAdmin && project.CreatedByRole == UserRole.Admin;
        }

        private Task ApplyProjectChangesAsync(Project project, UpdateProjectDto dto)
        {
            project.Name = dto.Name;
            project.Description = dto.Description;
            project.Status = dto.Status;
            project.Priority = dto.Priority;
            project.StartDate = dto.StartDate;
            project.EndDate = dto.EndDate;

            return _projectRepository.UpdateAsync(project);
        }

        // 202 Accepted: the request was understood and recorded, but nothing has
        // changed yet because it's sitting in the Admin's queue. Not an error,
        // so it isn't built through CreateErrorResponse.
        private static CommonResponse<T> PendingApprovalResponse<T>(ApprovalGateResult gate)
        {
            var message = gate.AlreadyPending
                ? $"This change is already waiting for Admin approval (request #{gate.ApprovalRequestId})."
                : $"This change needs Admin approval and has been sent for review (request #{gate.ApprovalRequestId}).";

            return new CommonResponse<T>
            {
                Success = true,
                Messages = new List<string> { message },
                Data = default,
                StatusCode = HttpStatusCode.Accepted
            };
        }
    }
}

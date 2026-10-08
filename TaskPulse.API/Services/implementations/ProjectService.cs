using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.ApprovalDto;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.ProjectDto;
using TaskPulse.API.DTO.ProjectMemberDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Helper.AuthorizationHelper;
using TaskPulse.API.Repository.ApprovalRepo;
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
        private readonly ApprovalRequestRepository _approvalRepository;
        private readonly IApprovalGate _approvalGate;
        private readonly IApprovalService _approvalService;
        private readonly IActivityLogger _activityLogger;
        private readonly INotifier _notifier;

        public ProjectService(
            CustomExceptionHandler customExceptionHandler,
            CheckValidationRun validationRunner,
            ProjectRepository projectRepository,
            UserRepository userRepository,
            ApprovalRequestRepository approvalRepository,
            IApprovalGate approvalGate,
            IApprovalService approvalService,
            IActivityLogger activityLogger,
            INotifier notifier)
            : base(customExceptionHandler)
        {
            _validationRunner = validationRunner;
            _projectRepository = projectRepository;
            _userRepository = userRepository;
            _approvalRepository = approvalRepository;
            _approvalGate = approvalGate;
            _approvalService = approvalService;
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


        // Reuses GetPagedAsync's existing membership-scoping — passing the
        // *target* user's id instead of the caller's is all "projects this
        // person belongs to" needs.
        public async Task<CommonResponse<PagedResult<ProjectDTO>>> GetProjectsForUserAsync(int userId)
        {
            var response = new CommonResponse<PagedResult<ProjectDTO>>();

            try
            {
                var query = new ProjectQueryDto { PageSize = 100 };
                var page = await _projectRepository.GetPagedAsync(query, userId);
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


        // Update Project Service Method — any project member can propose an
        // edit; it applies immediately for the project's creator (or an
        // Admin), and goes through the creator-resolved edit-request gate
        // for everyone else.
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

                var access = ProjectAccess.CheckMember(project, callerId, callerIsAdmin);
                if (access is not null)
                    return CommonResponseFactory.CreateErrorResponse<ProjectDTO>(access, HttpStatusCode.Forbidden);

                var gate = await _approvalGate.ExecuteOrRequestApprovalAsync(
                    ApprovalActionType.UpdateProject,
                    nameof(Project),
                    project.Id,
                    NeedsApproval(project, callerId, callerIsAdmin),
                    callerId,
                    () => ApplyProjectChangesAsync(project, dto),
                    dto);

                if (!gate.Executed)
                {
                    await NotifyCreatorOfPendingRequestAsync(project, gate);

                    // Unlike PendingApprovalResponse<T> below (used where there's
                    // no natural "unchanged" value to fall back to), a caller
                    // editing a project still expects a full ProjectDTO back —
                    // just the pre-edit one, since nothing was applied yet.
                    var pendingMessage = gate.AlreadyPending
                        ? $"This change is already waiting for the project creator's approval (request #{gate.ApprovalRequestId})."
                        : $"This change needs the project creator's approval and has been sent for review (request #{gate.ApprovalRequestId}).";
                    return CommonResponseFactory.CreateSuccessResponse(ProjectDTO.FromEntity(project), pendingMessage);
                }

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
                    NeedsApproval(project, callerId, callerIsAdmin),
                    callerId,
                    () => _projectRepository.DeleteAsync(project));

                if (!gate.Executed)
                {
                    await NotifyCreatorOfPendingRequestAsync(project, gate);
                    return PendingApprovalResponse<string>(gate);
                }

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


        // Pending UpdateProject requests on projects the caller created —
        // theirs to approve or reject, whatever their role.
        public async Task<CommonResponse<List<ApprovalRequestDTO>>> GetPendingEditRequestsForCreatorAsync(int callerId)
        {
            var response = new CommonResponse<List<ApprovalRequestDTO>>();

            try
            {
                var requests = await _approvalRepository.GetPendingForProjectCreatorAsync(callerId);
                return CommonResponseFactory.CreateSuccessResponse(requests.Select(ApprovalRequestDTO.FromEntity).ToList(), "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<List<ApprovalRequestDTO>>> GetSentEditRequestsAsync(int callerId)
        {
            var response = new CommonResponse<List<ApprovalRequestDTO>>();

            try
            {
                var requests = await _approvalRepository.GetSentByAsync(callerId, ApprovalActionType.UpdateProject);
                return CommonResponseFactory.CreateSuccessResponse(requests.Select(ApprovalRequestDTO.FromEntity).ToList(), "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        // Reuses ApprovalService's actual approve/reject logic (it doesn't
        // itself check who's calling) — only the authorization here differs
        // from the Admin queue: the project's creator may resolve this one too.
        public async Task<CommonResponse<ApprovalRequestDTO>> RespondToEditRequestAsync(int requestId, int callerId, bool callerIsAdmin, bool approve, string? note)
        {
            var response = new CommonResponse<ApprovalRequestDTO>();

            try
            {
                var request = await _approvalRepository.GetByIdAsync(requestId);
                if (request is null || request.ActionType != ApprovalActionType.UpdateProject)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>("Edit request not found.", HttpStatusCode.NotFound);

                var project = await _projectRepository.GetByIdAsync(request.TargetEntityId);
                if (project is null)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>("Project not found.", HttpStatusCode.NotFound);

                if (!callerIsAdmin && project.CreatedByUserId != callerId)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>(
                        "Only this project's creator can respond to this request.", HttpStatusCode.Forbidden);

                var dto = new ResolveApprovalDto { Note = note };
                return approve
                    ? await _approvalService.ApproveAsync(requestId, callerId, dto)
                    : await _approvalService.RejectAsync(requestId, callerId, dto);
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Only the project's own creator (or an Admin) can change/delete it
        // without asking first — everyone else needs that creator's sign-off.
        private static bool NeedsApproval(Project project, int callerId, bool callerIsAdmin)
        {
            return !callerIsAdmin && project.CreatedByUserId != callerId;
        }

        // The generic gate only pages Admins — the actual approver for one of
        // these is the project's creator, so tell them directly too.
        private async Task NotifyCreatorOfPendingRequestAsync(Project project, ApprovalGateResult gate)
        {
            if (gate.AlreadyPending)
                return;

            await _notifier.NotifyAsync(project.CreatedByUserId, NotificationType.ApprovalRequired,
                $"A change to '{project.Name}' is waiting for your approval.", nameof(Project), project.Id);
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
                ? $"This change is already waiting for the project creator's approval (request #{gate.ApprovalRequestId})."
                : $"This change needs the project creator's approval and has been sent for review (request #{gate.ApprovalRequestId}).";

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

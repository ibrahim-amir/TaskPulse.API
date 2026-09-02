using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.ApprovalDto;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Repository.ApprovalRepo;
using TaskPulse.API.Services.Infrastructure;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    public class ApprovalService : BaseServiceClass, IApprovalService
    {
        private readonly ApprovalRequestRepository _approvalRepository;
        private readonly IEnumerable<IApprovalActionHandler> _actionHandlers;
        private readonly INotifier _notifier;
        private readonly IActivityLogger _activityLogger;

        // Every handler registered in Program.cs arrives here; the right one is
        // picked by ActionType when a request is approved.
        public ApprovalService(
            CustomExceptionHandler customExceptionHandler,
            ApprovalRequestRepository approvalRepository,
            IEnumerable<IApprovalActionHandler> actionHandlers,
            INotifier notifier,
            IActivityLogger activityLogger)
            : base(customExceptionHandler)
        {
            _approvalRepository = approvalRepository;
            _actionHandlers = actionHandlers;
            _notifier = notifier;
            _activityLogger = activityLogger;
        }

        public async Task<CommonResponse<PagedResult<ApprovalRequestDTO>>> GetRequestsAsync(ApprovalQueryDto query)
        {
            var response = new CommonResponse<PagedResult<ApprovalRequestDTO>>();

            try
            {
                var page = await _approvalRepository.GetPagedAsync(query);
                return CommonResponseFactory.CreateSuccessResponse(page.MapItems(ApprovalRequestDTO.FromEntity), "Approval requests retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<ApprovalRequestDTO>> GetByIdAsync(int id)
        {
            var response = new CommonResponse<ApprovalRequestDTO>();

            try
            {
                var request = await _approvalRepository.GetByIdAsync(id);
                if (request is null)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>("Approval request not found.", HttpStatusCode.NotFound);

                return CommonResponseFactory.CreateSuccessResponse(ApprovalRequestDTO.FromEntity(request), "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<ApprovalRequestDTO>> ApproveAsync(int id, int adminUserId, ResolveApprovalDto dto)
        {
            var response = new CommonResponse<ApprovalRequestDTO>();

            try
            {
                var request = await _approvalRepository.GetByIdAsync(id);
                if (request is null)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>("Approval request not found.", HttpStatusCode.NotFound);

                if (request.Status != ApprovalStatus.Pending)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>(
                        $"This request was already {request.Status.ToString().ToLower()}.", HttpStatusCode.BadRequest);

                var handler = _actionHandlers.FirstOrDefault(h => h.ActionType == request.ActionType);
                if (handler is null)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>(
                        $"No handler is registered for '{request.ActionType}'.", HttpStatusCode.InternalServerError);

                // Carry out the original action first. If it throws, the request
                // stays Pending rather than being marked done on a failed change.
                await handler.ExecuteAsync(request);

                request.Status = ApprovalStatus.Approved;
                request.ResolvedDate = DateTime.UtcNow;
                request.ResolvedByUserId = adminUserId;
                request.ResolutionNote = dto.Note;
                await _approvalRepository.UpdateAsync(request);

                await _notifier.NotifyAsync(
                    request.RequestedByUserId,
                    NotificationType.ApprovalResolved,
                    $"Your request to {request.ActionType} on {request.TargetEntityType} #{request.TargetEntityId} was approved.",
                    nameof(ApprovalRequest),
                    request.Id);

                await _activityLogger.LogAsync(adminUserId, ActivityAction.ApprovalApproved, $"Approved request #{request.Id} ({request.ActionType}).");

                return CommonResponseFactory.CreateSuccessResponse(ApprovalRequestDTO.FromEntity(request), "Request approved and applied.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<ApprovalRequestDTO>> RejectAsync(int id, int adminUserId, ResolveApprovalDto dto)
        {
            var response = new CommonResponse<ApprovalRequestDTO>();

            try
            {
                var request = await _approvalRepository.GetByIdAsync(id);
                if (request is null)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>("Approval request not found.", HttpStatusCode.NotFound);

                if (request.Status != ApprovalStatus.Pending)
                    return CommonResponseFactory.CreateErrorResponse<ApprovalRequestDTO>(
                        $"This request was already {request.Status.ToString().ToLower()}.", HttpStatusCode.BadRequest);

                request.Status = ApprovalStatus.Rejected;
                request.ResolvedDate = DateTime.UtcNow;
                request.ResolvedByUserId = adminUserId;
                request.ResolutionNote = dto.Note;
                await _approvalRepository.UpdateAsync(request);

                await _notifier.NotifyAsync(
                    request.RequestedByUserId,
                    NotificationType.ApprovalResolved,
                    $"Your request to {request.ActionType} on {request.TargetEntityType} #{request.TargetEntityId} was declined.",
                    nameof(ApprovalRequest),
                    request.Id);

                await _activityLogger.LogAsync(adminUserId, ActivityAction.ApprovalRejected, $"Rejected request #{request.Id} ({request.ActionType}).");

                return CommonResponseFactory.CreateSuccessResponse(ApprovalRequestDTO.FromEntity(request), "Request declined.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }
    }
}

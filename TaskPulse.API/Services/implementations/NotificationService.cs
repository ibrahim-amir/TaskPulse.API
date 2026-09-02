using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.NotificationDto;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Repository.NotificationRepo;
using TaskPulse.API.Services.Infrastructure;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    public class NotificationService : BaseServiceClass, INotificationService
    {
        private readonly NotificationRepository _notificationRepository;

        public NotificationService(
            CustomExceptionHandler customExceptionHandler,
            NotificationRepository notificationRepository)
            : base(customExceptionHandler)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<CommonResponse<PagedResult<NotificationDTO>>> GetMineAsync(NotificationQueryDto query, int callerId)
        {
            var response = new CommonResponse<PagedResult<NotificationDTO>>();

            try
            {
                var page = await _notificationRepository.GetPagedForUserAsync(callerId, query);
                return CommonResponseFactory.CreateSuccessResponse(page.MapItems(NotificationDTO.FromEntity), "Notifications retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<int>> GetUnreadCountAsync(int callerId)
        {
            var response = new CommonResponse<int>();

            try
            {
                var count = await _notificationRepository.CountUnreadAsync(callerId);
                return CommonResponseFactory.CreateSuccessResponse(count, "Unread count retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<NotificationDTO>> MarkReadAsync(int id, int callerId)
        {
            var response = new CommonResponse<NotificationDTO>();

            try
            {
                var notification = await _notificationRepository.GetByIdAsync(id);

                // Not found and not-yours give the same answer, so this can't be
                // used to find out whether someone else's notification exists.
                if (notification is null || notification.UserId != callerId)
                    return CommonResponseFactory.CreateErrorResponse<NotificationDTO>("Notification not found.", HttpStatusCode.NotFound);

                await _notificationRepository.MarkReadAsync(notification);
                return CommonResponseFactory.CreateSuccessResponse(NotificationDTO.FromEntity(notification), "Marked as read.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<int>> MarkAllReadAsync(int callerId)
        {
            var response = new CommonResponse<int>();

            try
            {
                var updated = await _notificationRepository.MarkAllReadAsync(callerId);
                return CommonResponseFactory.CreateSuccessResponse(updated, $"{updated} notifications marked as read.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }
    }
}

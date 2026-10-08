using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.NotificationDto;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Helper.FileHelper;
using TaskPulse.API.Repository.NotificationRepo;
using TaskPulse.API.Repository.UserRepo;
using TaskPulse.API.Services.Infrastructure;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    public class NotificationService : BaseServiceClass, INotificationService
    {
        private readonly NotificationRepository _notificationRepository;
        private readonly UserRepository _userRepository;
        private readonly FileStorage _fileStorage;
        private readonly UploadedFileValidator _fileValidator;

        public NotificationService(
            CustomExceptionHandler customExceptionHandler,
            NotificationRepository notificationRepository,
            UserRepository userRepository,
            FileStorage fileStorage,
            UploadedFileValidator fileValidator)
            : base(customExceptionHandler)
        {
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _fileStorage = fileStorage;
            _fileValidator = fileValidator;
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

        public async Task<CommonResponse<NotificationDTO>> SendAsync(SendNotificationDto dto, int senderId)
        {
            var response = new CommonResponse<NotificationDTO>();

            try
            {
                if (string.IsNullOrWhiteSpace(dto.Message) && dto.Image is null)
                    return CommonResponseFactory.CreateErrorResponse<NotificationDTO>("Message or image is required.", HttpStatusCode.BadRequest);

                var recipient = await _userRepository.GetByIdAsync(dto.RecipientUserId);
                if (recipient is null)
                    return CommonResponseFactory.CreateErrorResponse<NotificationDTO>("User not found.", HttpStatusCode.NotFound);

                string? imageUrl = null;
                if (dto.Image is not null)
                {
                    var imageErrors = _fileValidator.ValidateImage(dto.Image);
                    if (imageErrors.Count > 0)
                        return CommonResponseFactory.CreateErrorResponse<NotificationDTO>(imageErrors, HttpStatusCode.BadRequest);

                    imageUrl = await _fileStorage.SaveChatImageAsync(dto.Image, senderId);
                }

                var sender = await _userRepository.GetByIdAsync(senderId);
                var senderName = sender?.Name ?? "Someone";
                var text = dto.Message?.Trim() ?? string.Empty;
                var formattedMessage = $"{senderName}: {text}".TrimEnd();

                // RelatedEntityType/Id point back at the sender, so the
                // recipient's client can offer "view sender" if it wants to.
                var recipientNotification = new Notification
                {
                    UserId = dto.RecipientUserId,
                    Type = NotificationType.DirectMessage,
                    Message = formattedMessage,
                    ImageUrl = imageUrl,
                    RelatedEntityType = "User",
                    RelatedEntityId = senderId,
                    IsRead = false,
                    IsOutgoing = false
                };
                await _notificationRepository.CreateAsync(recipientNotification);

                // A mirror copy on the sender's own side — Notification rows
                // only ever belong to their recipient, so without this the
                // sender could never see their own half of the conversation.
                // Already read (it's their own outgoing message).
                await _notificationRepository.CreateAsync(new Notification
                {
                    UserId = senderId,
                    Type = NotificationType.DirectMessage,
                    Message = formattedMessage,
                    ImageUrl = imageUrl,
                    RelatedEntityType = "User",
                    RelatedEntityId = dto.RecipientUserId,
                    IsRead = true,
                    IsOutgoing = true
                });

                return CommonResponseFactory.CreateSuccessResponse(NotificationDTO.FromEntity(recipientNotification), "Notification sent.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<string>> DeleteAsync(int id, int callerId)
        {
            var response = new CommonResponse<string>();

            try
            {
                var notification = await _notificationRepository.GetByIdAsync(id);

                // Same "not found and not-yours look identical" rule as MarkReadAsync.
                if (notification is null || notification.UserId != callerId)
                    return CommonResponseFactory.CreateErrorResponse<string>("Notification not found.", HttpStatusCode.NotFound);

                await _notificationRepository.DeleteAsync(notification);
                return CommonResponseFactory.CreateSuccessResponse("Notification deleted.", "Notification deleted.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<int>> ClearAllAsync(int callerId)
        {
            var response = new CommonResponse<int>();

            try
            {
                var deleted = await _notificationRepository.DeleteAllForUserAsync(callerId);
                return CommonResponseFactory.CreateSuccessResponse(deleted, $"{deleted} notification(s) cleared.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<int>> ClearConversationAsync(int callerId, int otherUserId)
        {
            var response = new CommonResponse<int>();

            try
            {
                var deleted = await _notificationRepository.DeleteConversationAsync(callerId, otherUserId);
                return CommonResponseFactory.CreateSuccessResponse(deleted, $"{deleted} message(s) cleared.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<List<NotificationDTO>>> GetConversationAsync(int callerId, int otherUserId)
        {
            var response = new CommonResponse<List<NotificationDTO>>();

            try
            {
                var thread = await _notificationRepository.GetConversationAsync(callerId, otherUserId);
                return CommonResponseFactory.CreateSuccessResponse(thread.Select(NotificationDTO.FromEntity).ToList(), "Conversation retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<List<ConversationSummaryDto>>> GetConversationsAsync(int callerId)
        {
            var response = new CommonResponse<List<ConversationSummaryDto>>();

            try
            {
                var conversations = await _notificationRepository.GetConversationSummariesAsync(callerId);
                return CommonResponseFactory.CreateSuccessResponse(conversations, "Conversations retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }
    }
}

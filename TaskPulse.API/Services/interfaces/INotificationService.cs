using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.DTO.NotificationDto;
using TaskPulse.API.DTO.PagingDto;

namespace TaskPulse.API.Services.interfaces
{
    // The read side. INotifier is the write side — services that cause a
    // notification use that one, this is only for showing them to the person
    // they belong to.
    public interface INotificationService
    {
        Task<CommonResponse<PagedResult<NotificationDTO>>> GetMineAsync(NotificationQueryDto query, int callerId);

        Task<CommonResponse<int>> GetUnreadCountAsync(int callerId);

        Task<CommonResponse<NotificationDTO>> MarkReadAsync(int id, int callerId);

        Task<CommonResponse<int>> MarkAllReadAsync(int callerId);

        // The one write this service exposes — a manual, user-composed
        // notification (e.g. from a profile page), not a system-raised one.
        Task<CommonResponse<NotificationDTO>> SendAsync(SendNotificationDto dto, int senderId);

        Task<CommonResponse<string>> DeleteAsync(int id, int callerId);

        Task<CommonResponse<int>> ClearAllAsync(int callerId);

        Task<CommonResponse<List<NotificationDTO>>> GetConversationAsync(int callerId, int otherUserId);

        // Deletes only the caller's own copy of the conversation — the other
        // person still sees theirs until they clear it too.
        Task<CommonResponse<int>> ClearConversationAsync(int callerId, int otherUserId);

        Task<CommonResponse<List<ConversationSummaryDto>>> GetConversationsAsync(int callerId);
    }
}

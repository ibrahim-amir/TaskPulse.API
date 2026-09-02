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
    }
}

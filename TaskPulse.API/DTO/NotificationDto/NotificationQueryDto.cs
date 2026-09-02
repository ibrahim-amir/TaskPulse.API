using TaskPulse.API.DTO.PagingDto;

namespace TaskPulse.API.DTO.NotificationDto;

// Query string for GET /api/notifications. Always scoped to the caller — the
// user id comes from the token, never the query string.
public class NotificationQueryDto : PagedRequest
{
    public bool? IsRead { get; set; }
}

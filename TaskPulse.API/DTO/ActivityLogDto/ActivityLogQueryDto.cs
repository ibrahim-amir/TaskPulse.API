using TaskPulse.API.DTO.PagingDto;

namespace TaskPulse.API.DTO.ActivityLogDto;

// Query string for GET /api/activity-logs. Search matches the action or its
// details; paging and sorting come from PagedRequest.
public class ActivityLogQueryDto : PagedRequest
{
    public int? UserId { get; set; }
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

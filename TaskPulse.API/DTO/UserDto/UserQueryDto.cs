using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.UserDto;

// Query string for GET /api/users. Search matches name or email;
// paging and sorting come from PagedRequest.
public class UserQueryDto : PagedRequest
{
    public UserRole? Role { get; set; }
    public bool? IsDisabled { get; set; }
}

using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.Entities.EnumForEntities;

namespace TaskPulse.API.DTO.ProjectDto;

// Query string for GET /api/projects. Search matches name or description;
// paging and sorting come from PagedRequest.
public class ProjectQueryDto : PagedRequest
{
    public ProjectStatus? Status { get; set; }
    public PriorityLevel? Priority { get; set; }
}

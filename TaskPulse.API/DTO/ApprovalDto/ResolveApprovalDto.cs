namespace TaskPulse.API.DTO.ApprovalDto;

// Body for approve and reject. The note is optional on approve and is where a
// rejection reason goes, so the Manager who asked can see why.
public class ResolveApprovalDto
{
    public string? Note { get; set; }
}

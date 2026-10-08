namespace TaskPulse.API.DTO.TaskApprovalDto
{
    public class CreateTaskApprovalRequestDto
    {
        public string? Description { get; set; }
        public List<int> ApproverUserIds { get; set; } = new();
    }
}

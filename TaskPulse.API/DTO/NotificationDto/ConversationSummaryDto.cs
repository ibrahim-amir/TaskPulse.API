namespace TaskPulse.API.DTO.NotificationDto;

// One row per person the caller has exchanged DirectMessage notifications
// with — powers the chat-style conversation list.
public class ConversationSummaryDto
{
    public int OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserProfileImageUrl { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}

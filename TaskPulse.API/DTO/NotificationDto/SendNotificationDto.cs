using Microsoft.AspNetCore.Http;

namespace TaskPulse.API.DTO.NotificationDto;

// Request body for POST /api/users/{id}/notifications — a manual,
// user-composed notification, distinct from the system-raised ones.
// Multipart (not JSON) so Image can ride along as a form field.
public class SendNotificationDto
{
    // Filled from the route by the controller, not sent in the body.
    public int RecipientUserId { get; set; }

    public string? Message { get; set; }
    public IFormFile? Image { get; set; }
}

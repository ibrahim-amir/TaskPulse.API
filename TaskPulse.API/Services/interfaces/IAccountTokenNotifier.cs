using TaskPulse.API.Entities;

namespace TaskPulse.API.Services.interfaces
{
    public interface IAccountTokenNotifier
    {
        string IssueEmailConfirmationToken(User user);
        Task SendEmailConfirmationAsync(string email, string token);
        string IssuePasswordResetToken(User user);
        Task SendPasswordResetEmailAsync(string email, string token);
    }
}

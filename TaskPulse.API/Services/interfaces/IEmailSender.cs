namespace TaskPulse.API.Services.interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string body);
    }
}

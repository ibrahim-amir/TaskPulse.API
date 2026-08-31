using Microsoft.Extensions.Logging;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    public class LoggingEmailSender : IEmailSender
    {
        private readonly ILogger<LoggingEmailSender> _logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string toEmail, string subject, string body)
        {
            _logger.LogInformation("EMAIL to {ToEmail} | Subject: {Subject}\n{Body}", toEmail, subject, body);
            return Task.CompletedTask;
        }
    }
}

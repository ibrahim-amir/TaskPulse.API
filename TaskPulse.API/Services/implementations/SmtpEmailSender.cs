using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    // Sends real mail over Gmail SMTP via MailKit — still logs every send the
    // same way LoggingEmailSender did, so nothing is lost from the console/
    // file logs, but the message now actually reaches the recipient too.
    //
    // Host/Port/FromAddress/FromName are plain config (appsettings.json).
    // The Gmail account itself (Smtp:Username) and its App Password
    // (Smtp:AppPassword) are secrets — read from IConfiguration, backed by
    // user-secrets in Development and whatever secret store is configured
    // elsewhere. Never checked into appsettings.json.
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            _logger.LogInformation("EMAIL to {ToEmail} | Subject: {Subject}\n{Body}", toEmail, subject, body);

            var username = _configuration["Smtp:Username"];
            var appPassword = _configuration["Smtp:AppPassword"];

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(appPassword))
            {
                _logger.LogWarning(
                    "Smtp:Username / Smtp:AppPassword are not configured — the email to {ToEmail} was logged but not actually sent.",
                    toEmail);
                return;
            }

            var host = _configuration["Smtp:Host"] ?? "smtp.gmail.com";
            var port = _configuration.GetValue("Smtp:Port", 587);
            var fromAddress = _configuration["Smtp:FromAddress"];
            var fromName = _configuration["Smtp:FromName"] ?? "TaskPulse";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, string.IsNullOrWhiteSpace(fromAddress) ? username : fromAddress));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            // MimeMessage doesn't set these on its own — a missing Date/
            // Message-Id is a classic spam-filter signal, and a from==to
            // self-send (the common local test case) is exactly the kind of
            // mail Gmail is most likely to quietly file under Spam without
            // either of them.
            message.Date = DateTimeOffset.Now;
            message.MessageId = MimeKit.Utils.MimeUtils.GenerateMessageId();

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, appPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email to {ToEmail} sent via SMTP.", toEmail);
            }
            catch (Exception ex)
            {
                // A delivery hiccup (bad credentials, Gmail rate limit, no
                // network) shouldn't take down the caller's main flow — e.g.
                // registering a user still succeeds even if the confirmation
                // email fails to go out right this moment.
                _logger.LogError(ex, "Failed to send email to {ToEmail} via SMTP.", toEmail);
            }
        }
    }
}

using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Tests.Infrastructure
{
    // Stands in for LoggingEmailSender during tests. The real one writes the
    // confirmation and reset tokens to a log file; this keeps them in memory so
    // a test can read the token back and finish the flow, the same way a person
    // would click the link in the email.
    public class TestEmailSender : IEmailSender
    {
        private readonly List<SentEmail> _sent = new();
        private readonly object _gate = new();

        public Task SendAsync(string to, string subject, string body)
        {
            lock (_gate)
            {
                _sent.Add(new SentEmail(to, subject, body));
            }

            return Task.CompletedTask;
        }

        public SentEmail? LastTo(string email)
        {
            lock (_gate)
            {
                return _sent.LastOrDefault(sent => string.Equals(sent.To, email, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Both notifier messages end with ": {token}", so the token is whatever
        // follows the last colon.
        public string? LastTokenFor(string email)
        {
            var body = LastTo(email)?.Body;
            if (string.IsNullOrWhiteSpace(body))
                return null;

            var separator = body.LastIndexOf(':');
            return separator < 0 ? null : body[(separator + 1)..].Trim();
        }

        public void Clear()
        {
            lock (_gate)
            {
                _sent.Clear();
            }
        }
    }

    public record SentEmail(string To, string Subject, string Body);
}

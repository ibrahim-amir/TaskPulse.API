using TaskPulse.API.Authentication;
using TaskPulse.API.Entities;
using TaskPulse.API.Helper.TokenHelperClass;
using TaskPulse.API.Services.interfaces;

namespace TaskPulse.API.Services.implementations
{
    public class AccountTokenNotifier : IAccountTokenNotifier
    {
        private readonly TokenService _tokenService;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public AccountTokenNotifier(TokenService tokenService, IEmailSender emailSender, IConfiguration configuration)
        {
            _tokenService = tokenService;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        public string IssueEmailConfirmationToken(User user)
        {
            var (token, expiresAt) = _tokenService.GenerateEmailConfirmationToken();
            user.EmailConfirmationTokenHash = TokenHasher.HashToken(token);
            user.EmailConfirmationTokenExpiresAt = expiresAt;
            return token;
        }

        public Task SendEmailConfirmationAsync(string email, string token)
        {
            var confirmUrl = BuildFrontendUrl("confirm-email", email, token);
            var expiryHours = _configuration["EmailConfirmation:TokenExpiryHours"] ?? "24";

            var body = EmailTemplate.Render(
                heading: "Confirm your TaskPulse account",
                intro: "Click the button below to confirm your email address and finish setting up your account.",
                buttonLabel: "Confirm email",
                actionUrl: confirmUrl,
                footnote: $"This link expires in {expiryHours} hours. If you didn't request this, you can safely ignore this email.");

            return _emailSender.SendAsync(email, "Confirm your TaskPulse account", body);
        }

        public string IssuePasswordResetToken(User user)
        {
            var (token, expiresAt) = _tokenService.GeneratePasswordResetToken();
            user.PasswordResetTokenHash = TokenHasher.HashToken(token);
            user.PasswordResetTokenExpiresAt = expiresAt;
            return token;
        }

        public Task SendPasswordResetEmailAsync(string email, string token)
        {
            var resetUrl = BuildFrontendUrl("reset-password", email, token);
            var expiryMinutes = _configuration["PasswordReset:TokenExpiryMinutes"] ?? "30";

            var body = EmailTemplate.Render(
                heading: "Reset your TaskPulse password",
                intro: "Click the button below to choose a new password for your account.",
                buttonLabel: "Reset password",
                actionUrl: resetUrl,
                footnote: $"This link expires in {expiryMinutes} minutes. If you didn't request this, you can safely ignore this email.");

            return _emailSender.SendAsync(email, "Reset your TaskPulse password", body);
        }

        // Both flows land on a frontend page that reads ?email=&token= and
        // calls the matching API itself — see ConfirmEmailStatus.tsx /
        // ResetPasswordForm.tsx. Frontend:BaseUrl is the one place that
        // changes between local dev and a real deployment.
        private string BuildFrontendUrl(string path, string email, string token)
        {
            var baseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
            return $"{baseUrl}/{path}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        }
    }
}

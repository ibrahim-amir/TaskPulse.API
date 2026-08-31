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

        public AccountTokenNotifier(TokenService tokenService, IEmailSender emailSender)
        {
            _tokenService = tokenService;
            _emailSender = emailSender;
        }

        public string IssueEmailConfirmationToken(User user)
        {
            var (token, expiresAt) = _tokenService.GenerateEmailConfirmationToken();
            user.EmailConfirmationTokenHash = TokenHasher.HashToken(token);
            user.EmailConfirmationTokenExpiresAt = expiresAt;
            return token;
        }

        public Task SendEmailConfirmationAsync(string email, string token) =>
            _emailSender.SendAsync(
                email,
                "Confirm your TaskPulse account",
                $"Confirm your email with this token: {token}");

        public string IssuePasswordResetToken(User user)
        {
            var (token, expiresAt) = _tokenService.GeneratePasswordResetToken();
            user.PasswordResetTokenHash = TokenHasher.HashToken(token);
            user.PasswordResetTokenExpiresAt = expiresAt;
            return token;
        }

        public Task SendPasswordResetEmailAsync(string email, string token) =>
            _emailSender.SendAsync(
                email,
                "Reset your TaskPulse password",
                $"Reset your password with this token: {token}");
    }
}

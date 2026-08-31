using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Helper.TokenHelperClass;

namespace TaskPulse.API.Authentication
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string GenerateToken(User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"]!);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetAccessTokenExpiry()
        {
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"]!);
            return DateTime.UtcNow.AddMinutes(expiryMinutes);
        }

        // Raw token to hand to the client + when it expires. Caller is responsible
        // for storing only TokenHasher.HashToken(token) against the user.
        public (string Token, DateTime ExpiresAt) GenerateRefreshToken(bool rememberMe)
        {
            var token = TokenHasher.GenerateToken();
            var days = rememberMe
                ? int.Parse(_configuration["Jwt:RememberMeRefreshTokenExpiryDays"] ?? "30")
                : int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");

            return (token, DateTime.UtcNow.AddDays(days));
        }
        public AuthResponseDto IssueAuthTokens(User user, bool rememberMe)
        {
            var accessToken = GenerateToken(user);
            var accessTokenExpiresAt = GetAccessTokenExpiry();
            var (refreshToken, refreshTokenExpiresAt) = GenerateRefreshToken(rememberMe);

            user.RefreshTokenHash = TokenHasher.HashToken(refreshToken);
            user.RefreshTokenExpiresAt = refreshTokenExpiresAt;
            user.RefreshTokenRememberMe = rememberMe;

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshTokenExpiresAt
            };
        }
        public (string Token, DateTime ExpiresAt) GenerateEmailConfirmationToken()
        {
            var token = TokenHasher.GenerateToken();
            var hours = int.Parse(_configuration["EmailConfirmation:TokenExpiryHours"] ?? "24");
            return (token, DateTime.UtcNow.AddHours(hours));
        }
        public (string Token, DateTime ExpiresAt) GeneratePasswordResetToken()
        {
            var token = TokenHasher.GenerateToken();
            var minutes = int.Parse(_configuration["PasswordReset:TokenExpiryMinutes"] ?? "30");
            return (token, DateTime.UtcNow.AddMinutes(minutes));
        }
    }
}

using System.Net;
using Microsoft.AspNetCore.Http;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.PagingDto;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Services.interfaces;
using TaskPulse.API.Services.Infrastructure;
using TaskPulse.API.Validation;
using TaskPulse.API.Repository.UserRepo;
using TaskPulse.API.Authentication;
using TaskPulse.API.Helper.FileHelper;
using TaskPulse.API.Helper.PasswordHelperClass;
using TaskPulse.API.Helper.TokenHelperClass;

namespace TaskPulse.API.Services.implementations
{
    public class UserService : BaseServiceClass, IUserServices
    {
        private readonly CheckValidationRun _validationRunner;
        private readonly UserRepository _userRepository;
        private readonly TokenService _tokenService;
        private readonly IAccountTokenNotifier _accountTokenNotifier;
        private readonly IActivityLogger _activityLogger;
        private readonly FileStorage _fileStorage;
        private readonly UploadedFileValidator _fileValidator;

        public UserService(
            CustomExceptionHandler customExceptionHandler,
            CheckValidationRun validationRunner,
            UserRepository userRepository,
            TokenService tokenService,
            IAccountTokenNotifier accountTokenNotifier,
            IActivityLogger activityLogger,
            FileStorage fileStorage,
            UploadedFileValidator fileValidator)
            : base(customExceptionHandler)
        {
            _validationRunner = validationRunner;
            _userRepository = userRepository;
            _tokenService = tokenService;
            _accountTokenNotifier = accountTokenNotifier;
            _activityLogger = activityLogger;
            _fileStorage = fileStorage;
            _fileValidator = fileValidator;
        }



        // Create User Service Method
        public async Task<CommonResponse<UserDTO>> CreateUserAsync(CreateUserDto dto)
        {
            var response = new CommonResponse<UserDTO>();

            try
            {
                // Get DTO Validated
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<UserDTO>(validation.Errors, validation.StatusCode);


                // Creating a User Entity from the DTO.
                var user = User.CreateUserFromDto(dto);
                user.PasswordHash = PasswordHelper.HashPassword(dto.Password); // Creating a password Hash to store.

                // Admin set this password directly (there's no self-signup), so
                // treat it as temporary until the user changes it themselves.
                user.MustChangePassword = true;

                var confirmationToken = _accountTokenNotifier.IssueEmailConfirmationToken(user);
                await _userRepository.CreateUserAsync(user);
                await _accountTokenNotifier.SendEmailConfirmationAsync(user.Email, confirmationToken);
                await _activityLogger.LogAsync(user.Id, ActivityAction.UserCreated, $"Account created with role {user.Role}.");

                return CommonResponseFactory.CreateSuccessResponse(UserDTO.CreateUserDtoFromUserEntity(user), CommonResponseMessage.Created.ToText());
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Get User By Id Service Method
        public async Task<CommonResponse<UserDTO>> GetUserById(int id)
        {
            var response = new CommonResponse<UserDTO>();
            try
            {
                //Get user by id from repo.
                var user = await _userRepository.GetByIdAsync(id);

                if (user is null)
                    return CommonResponseFactory.CreateErrorResponse<UserDTO>("User not found.", HttpStatusCode.NotFound);

                return CommonResponseFactory.CreateSuccessResponse(UserDTO.CreateUserDtoFromUserEntity(user), "Found.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        public async Task<CommonResponse<PagedResult<UserDTO>>> GetUsersAsync(UserQueryDto query)
        {
            var response = new CommonResponse<PagedResult<UserDTO>>();

            try
            {
                var page = await _userRepository.GetPagedAsync(query);
                var mappedPage = page.MapItems(UserDTO.CreateUserDtoFromUserEntity);

                return CommonResponseFactory.CreateSuccessResponse(mappedPage, "Users retrieved.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Update User Profile Service Method — name and image only.
        public async Task<CommonResponse<UserDTO>> UpdateUserProfileAsync(UpdateUserProfileDto dto)
        {
            var response = new CommonResponse<UserDTO>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<UserDTO>(validation.Errors, validation.StatusCode);

                var user = await _userRepository.GetByIdAsync(dto.Id);
                if (user is null)
                    return CommonResponseFactory.CreateErrorResponse<UserDTO>("User not found.", HttpStatusCode.NotFound);

                user.Name = dto.Name;
                user.profileImageUrl = dto.profileImageUrl;
                user.IsAvailableForAssignment = dto.IsAvailableForAssignment;
                await _userRepository.UpdateUserAsync(user);
                await _activityLogger.LogAsync(user.Id, ActivityAction.UserProfileUpdated, "Profile updated.");

                return CommonResponseFactory.CreateSuccessResponse(UserDTO.CreateUserDtoFromUserEntity(user), "Profile updated.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Update Avatar Service Method — stores the file under wwwroot/avatars
        // (served as a plain static URL) and points profileImageUrl at it.
        public async Task<CommonResponse<UserDTO>> UpdateAvatarAsync(int userId, IFormFile file)
        {
            var response = new CommonResponse<UserDTO>();

            try
            {
                var fileErrors = _fileValidator.ValidateImage(file);
                if (fileErrors.Count > 0)
                    return CommonResponseFactory.CreateErrorResponse<UserDTO>(fileErrors, HttpStatusCode.BadRequest);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user is null)
                    return CommonResponseFactory.CreateErrorResponse<UserDTO>("User not found.", HttpStatusCode.NotFound);

                user.profileImageUrl = await _fileStorage.SaveAvatarAsync(file!, userId);
                await _userRepository.UpdateUserAsync(user);
                await _activityLogger.LogAsync(user.Id, ActivityAction.UserProfileUpdated, "Profile picture updated.");

                return CommonResponseFactory.CreateSuccessResponse(UserDTO.CreateUserDtoFromUserEntity(user), "Avatar updated.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Update User Role Service Method — Admin only, guarded against
        // demoting the last Admin (validation layer).
        public async Task<CommonResponse<UserDTO>> UpdateUserRoleAsync(UpdateUserRoleDto dto)
        {
            var response = new CommonResponse<UserDTO>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<UserDTO>(validation.Errors, validation.StatusCode);

                var user = await _userRepository.GetByIdAsync(dto.Id);
                if (user is null)
                    return CommonResponseFactory.CreateErrorResponse<UserDTO>("User not found.", HttpStatusCode.NotFound);

                user.Role = dto.Role;
                await _userRepository.UpdateUserAsync(user);
                await _activityLogger.LogAsync(user.Id, ActivityAction.UserRoleChanged, $"Role set to {user.Role}.");

                return CommonResponseFactory.CreateSuccessResponse(UserDTO.CreateUserDtoFromUserEntity(user), "Role updated.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Set User Status Service Method — backs both PUT /status and DELETE.
        // Disabling also clears the refresh token, so the account can't be kept
        // alive by refreshing after it's been switched off.
        public async Task<CommonResponse<UserDTO>> SetUserStatusAsync(UpdateUserStatusDto dto)
        {
            var response = new CommonResponse<UserDTO>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<UserDTO>(validation.Errors, validation.StatusCode);

                var user = await _userRepository.GetByIdAsync(dto.Id);
                if (user is null)
                    return CommonResponseFactory.CreateErrorResponse<UserDTO>("User not found.", HttpStatusCode.NotFound);

                user.IsDisabled = dto.IsDisabled;

                if (dto.IsDisabled)
                {
                    user.RefreshTokenHash = null;
                    user.RefreshTokenExpiresAt = null;
                    user.RefreshTokenRememberMe = false;
                }

                await _userRepository.UpdateUserAsync(user);
                await _activityLogger.LogAsync(user.Id,ActivityAction.UserStatusChanged,dto.IsDisabled ? "Account disabled." : "Account enabled.");

                return CommonResponseFactory.CreateSuccessResponse(UserDTO.CreateUserDtoFromUserEntity(user),dto.IsDisabled ? "Account disabled." : "Account enabled.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Delete User Service Method — a real row delete, distinct from
        // SetUserStatusAsync(IsDisabled: true). Only proceeds when the account
        // has no history; otherwise the caller is told exactly what's in the
        // way and pointed at disabling instead.
        public async Task<CommonResponse<string>> DeleteUserAsync(int id, int callerId)
        {
            var response = new CommonResponse<string>();

            try
            {
                if (id == callerId)
                    return CommonResponseFactory.CreateErrorResponse<string>("You can't delete your own account.", HttpStatusCode.BadRequest);

                var user = await _userRepository.GetByIdAsync(id);
                if (user is null)
                    return CommonResponseFactory.CreateErrorResponse<string>("User not found.", HttpStatusCode.NotFound);

                if (user.Role == UserRole.Admin && await _userRepository.CountActiveAdminsAsync() <= 1)
                    return CommonResponseFactory.CreateErrorResponse<string>(
                        "This is the last active Admin — promote another user to Admin first.", HttpStatusCode.BadRequest);

                var blockers = await _userRepository.GetDeletionBlockersAsync(id);
                if (blockers.Count > 0)
                    return CommonResponseFactory.CreateErrorResponse<string>(
                        $"Can't delete — this account has {string.Join(", ", blockers)}. Disable it instead.",
                        HttpStatusCode.BadRequest);

                await _userRepository.DeleteAsync(user);
                await _activityLogger.LogAsync(callerId, ActivityAction.UserDeleted, $"Deleted user {user.Email}.");

                return CommonResponseFactory.CreateSuccessResponse("User deleted.", "User deleted.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Login Service Method
        public async Task<CommonResponse<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            var response = new CommonResponse<AuthResponseDto>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<AuthResponseDto>(validation.Errors, validation.StatusCode);

                var user = await _userRepository.GetByEmailAsync(dto.Email);
                if (user is null)
                    return CommonResponseFactory.CreateErrorResponse<AuthResponseDto>("Invalid email or password.", HttpStatusCode.Unauthorized);

                var authResponse = _tokenService.IssueAuthTokens(user, dto.RememberMe);
                await _userRepository.UpdateUserAsync(user);

                //Attendance is read back off these rows — one login is one day present.
                await _activityLogger.LogAsync(user.Id, ActivityAction.Login, "Signed in.");

                return CommonResponseFactory.CreateSuccessResponse(authResponse, "Login successful.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Refresh Token Service Method
        public async Task<CommonResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            var response = new CommonResponse<AuthResponseDto>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<AuthResponseDto>(validation.Errors, validation.StatusCode);

                var user = await _userRepository.GetByRefreshTokenHashAsync(TokenHasher.HashToken(dto.RefreshToken));
                if (user is null)
                    return CommonResponseFactory.CreateErrorResponse<AuthResponseDto>("Refresh token is invalid or expired.", HttpStatusCode.Unauthorized);

                var authResponse = _tokenService.IssueAuthTokens(user, user.RefreshTokenRememberMe);
                await _userRepository.UpdateUserAsync(user);

                return CommonResponseFactory.CreateSuccessResponse(authResponse, "Token refreshed.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Logout Service Method
        public async Task<CommonResponse<string>> LogoutAsync(int userId)
        {
            var response = new CommonResponse<string>();

            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user is null)
                    return CommonResponseFactory.CreateErrorResponse<string>("User not found.", HttpStatusCode.NotFound);

                user.RefreshTokenHash = null;
                user.RefreshTokenExpiresAt = null;
                user.RefreshTokenRememberMe = false;
                await _userRepository.UpdateUserAsync(user);
                await _activityLogger.LogAsync(user.Id, ActivityAction.Logout, "Signed out.");

                return CommonResponseFactory.CreateSuccessResponse("Logged out.", "Logged out successfully.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Confirm Email Service Method
        public async Task<CommonResponse<string>> ConfirmEmailAsync(ConfirmEmailDto dto)
        {
            var response = new CommonResponse<string>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<string>(validation.Errors, validation.StatusCode);

                var user = await _userRepository.GetByEmailAsync(dto.Email);
                if (user is null)
                    return CommonResponseFactory.CreateErrorResponse<string>("Invalid email or confirmation token.", HttpStatusCode.BadRequest);

                user.IsEmailConfirmed = true;
                user.EmailConfirmationTokenHash = null;
                user.EmailConfirmationTokenExpiresAt = null;
                await _userRepository.UpdateUserAsync(user);
                await _activityLogger.LogAsync(user.Id, ActivityAction.EmailConfirmed, "Email confirmed.");

                return CommonResponseFactory.CreateSuccessResponse("Email confirmed.", "Your email has been confirmed.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<string>> ResendConfirmationAsync(ResendConfirmationDto dto)
        {
            var response = new CommonResponse<string>();
            const string genericMessage = "If that email is registered and not yet confirmed, a new confirmation link has been sent.";

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<string>(validation.Errors, validation.StatusCode);

                var user = await _userRepository.GetByEmailAsync(dto.Email);
                if (user is not null && !user.IsEmailConfirmed)
                {
                    var confirmationToken = _accountTokenNotifier.IssueEmailConfirmationToken(user);
                    await _userRepository.UpdateUserAsync(user);
                    await _accountTokenNotifier.SendEmailConfirmationAsync(user.Email, confirmationToken);
                }
                return CommonResponseFactory.CreateSuccessResponse(genericMessage, genericMessage);
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Forgot Password Service Method
        public async Task<CommonResponse<string>> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var response = new CommonResponse<string>();
            const string genericMessage = "If that email is registered, a password reset link has been sent.";

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<string>(validation.Errors, validation.StatusCode);

                var user = await _userRepository.GetByEmailAsync(dto.Email);
                if (user is not null)
                {
                    var resetToken = _accountTokenNotifier.IssuePasswordResetToken(user);
                    await _userRepository.UpdateUserAsync(user);
                    await _accountTokenNotifier.SendPasswordResetEmailAsync(user.Email, resetToken);
                }

                return CommonResponseFactory.CreateSuccessResponse(genericMessage, genericMessage);
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Reset Password Service Method
        public async Task<CommonResponse<string>> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var response = new CommonResponse<string>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<string>(validation.Errors, validation.StatusCode);

                var user = await _userRepository.GetByEmailAsync(dto.Email);
                if (user is null)
                    return CommonResponseFactory.CreateErrorResponse<string>("Invalid or expired reset link.", HttpStatusCode.BadRequest);

                user.PasswordHash = PasswordHelper.HashPassword(dto.NewPassword);
                user.PasswordResetTokenHash = null;
                user.PasswordResetTokenExpiresAt = null;

                user.RefreshTokenHash = null;
                user.RefreshTokenExpiresAt = null;
                user.RefreshTokenRememberMe = false;

                await _userRepository.UpdateUserAsync(user);
                await _activityLogger.LogAsync(user.Id, ActivityAction.PasswordReset, "Password reset.");

                return CommonResponseFactory.CreateSuccessResponse("Password reset.", "Your password has been reset. Please log in again.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Change Password Service Method — the logged-in-user path (current
        // password required), distinct from ResetPasswordAsync's forgot-password
        // token path. Also how MustChangePassword gets cleared.
        public async Task<CommonResponse<string>> ChangePasswordAsync(ChangePasswordDto dto)
        {
            var response = new CommonResponse<string>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<string>(validation.Errors, validation.StatusCode);

                var user = await _userRepository.GetByIdAsync(dto.Id);
                if (user is null)
                    return CommonResponseFactory.CreateErrorResponse<string>("User not found.", HttpStatusCode.NotFound);

                user.PasswordHash = PasswordHelper.HashPassword(dto.NewPassword);
                user.MustChangePassword = false;
                await _userRepository.UpdateUserAsync(user);
                await _activityLogger.LogAsync(user.Id, ActivityAction.PasswordReset, "Password changed.");

                return CommonResponseFactory.CreateSuccessResponse("Password changed.", "Your password has been changed.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }
    }
}

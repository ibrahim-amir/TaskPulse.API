using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Services.interfaces;
using TaskPulse.API.Services.Infrastructure;
using TaskPulse.API.Validation;
using TaskPulse.API.Repository.UserRepo;
using TaskPulse.API.Authentication;
using TaskPulse.API.Helper.PasswordHelperClass;

namespace TaskPulse.API.Services.implementations
{
    public class UserService : BaseServiceClass, IUserServices
    {
        private readonly CheckValidationRun _validationRunner;
        private readonly UserRepository _userRepository;
        private readonly TokenService _tokenService;

        public UserService(CustomExceptionHandler customExceptionHandler, CheckValidationRun validationRunner, UserRepository userRepository, TokenService tokenService)
        : base(customExceptionHandler)
        {
            _validationRunner = validationRunner;
            _userRepository = userRepository;
            _tokenService = tokenService;
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

                await _userRepository.CreateUserAsync(user);

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


        // Login Service Method
        public async Task<CommonResponse<string>> LoginAsync(LoginDto dto)
        {
            var response = new CommonResponse<string>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<string>(validation.Errors, validation.StatusCode);

                var user = await _userRepository.GetByEmailAsync(dto.Email);
                if (user is null)
                    return CommonResponseFactory.CreateErrorResponse<string>("Invalid email or password.", HttpStatusCode.Unauthorized);

                var token = _tokenService.GenerateToken(user);
                return CommonResponseFactory.CreateSuccessResponse(token, "Login successful.");
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }
    }
}

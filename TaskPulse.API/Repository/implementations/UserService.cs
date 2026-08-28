using System.Net;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.CommonResponseMessages;
using TaskPulse.API.CommonResponseMessageExtensions;
using TaskPulse.API.Data;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Repository.interfaces;
using TaskPulse.API.Repository.Infrastructure;
using TaskPulse.API.Validation;

namespace TaskPulse.API.Repository.implementations
{
    public class UserService : BaseServiceClass, IUserServices
    {
        private readonly AppDbContext dbContext;
        private readonly CheckValidationRun _validationRunner;

        public UserService(AppDbContext dbContext, CustomExceptionHandler customExceptionHandler, CheckValidationRun validationRunner)
            : base(customExceptionHandler)
        {
            this.dbContext = dbContext;
            _validationRunner = validationRunner;
        }



        public async Task<CommonResponse<UserDTO>> CreateUserAsync(CreateUserDto dto)
        {
            var response = new CommonResponse<UserDTO>();

            try
            {
                var validation = await _validationRunner.Run(dto);
                if (!validation.Success)
                    return CommonResponseFactory.CreateErrorResponse<UserDTO>(validation.Errors, validation.StatusCode);

                var user = User.CreateUserFromDto(dto);
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password); // Creating a password Hash to store.

                // Storing the actual data using Db context.
                await dbContext.Users.AddAsync(user);
                await dbContext.SaveChangesAsync();

                return CommonResponseFactory.CreateSuccessResponse(UserDTO.FromEntity(user), CommonResponseMessage.Created.ToText());
            }
            catch (Exception ex)
            {
                // CustomExceptionHandler gets the first catch — GlobalExceptionHandler
                // stays the fallback for whatever isn't caught here.
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }

        public async Task<CommonResponse<User>> GetUserById(int id)
        {
            var response = new CommonResponse<User>();

            try
            {
                if (id <= 0)
                {
                    return response.ReturnError(HttpStatusCode.NotFound, CommonResponseMessage.NotFound.ToText());
                }
                var user = await dbContext.Users.FindAsync(id);
                if (user == null)
                {
                    return response.ReturnError(HttpStatusCode.NotFound, CommonResponseMessage.NotFound.ToText());
                }
                response.Data = user;
                return response;

            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


    }
}

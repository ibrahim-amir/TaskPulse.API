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
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password); // Creating a password Hash to store.

                // Storing the actual data using Db context.
                await dbContext.Users.AddAsync(user);
                await dbContext.SaveChangesAsync();

                return CommonResponseFactory.CreateSuccessResponse(UserDTO.CreateUserDtoFromUserEntity(user), CommonResponseMessage.Created.ToText());
            }
            catch (Exception ex)
            {
                return await _customExceptionHandler.HandleAsync(ex, CommonResponseMessage.ServerError.ToText(), response);
            }
        }


        // Get User By Id Service Method
        public async Task<CommonResponse<User>> GetUserById(int id)
        {
            var response = new CommonResponse<User>();

            try
            {// Should be a validation instead.
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

        Task<CommonResponse<UserDTO>> IUserServices.GetUserById(int id)
        {
            throw new NotImplementedException();
        }
    }
}

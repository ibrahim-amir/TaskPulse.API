using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using TaskPulse.API.CommonResponseClass;
using TaskPulse.API.Data;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.Entities.EnumForEntities;
using TaskPulse.API.Validation;

public class UserValidation : IValidation<CreateUserDto>
{
    private readonly AppDbContext _dbContext;
    public UserValidation(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<CommonResponse<CreateUserDto>> Validate(CreateUserDto obj)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(obj.Name)) errors.Add("Name is required.");
        if (string.IsNullOrWhiteSpace(obj.Email)) errors.Add("Email is required.");
        if (string.IsNullOrWhiteSpace(obj.Password)) errors.Add("Password is required.");
        if (!Enum.IsDefined(typeof(UserRole), obj.Role)) errors.Add($"'{obj.Role}' is not a valid role.");

        // password strength checks unchanged...

        if (!string.IsNullOrWhiteSpace(obj.Email))
        {
            try
            {
                var addr = new MailAddress(obj.Email);
                if (addr.Address != obj.Email) errors.Add("Email is not in a valid format.");
            }
            catch (FormatException)
            {
                errors.Add("Email is not in a valid format.");
            }

            if (errors.Count == 0 && await _dbContext.Users.AnyAsync(u => u.Email == obj.Email))
                errors.Add("Email is already registered.");
        }

        if (errors.Count > 0)
            return CommonResponseFactory.CreateErrorResponse<CreateUserDto>(errors, HttpStatusCode.BadRequest);

        return CommonResponseFactory.CreateSuccessResponse(obj, "Valid.");
    }
}

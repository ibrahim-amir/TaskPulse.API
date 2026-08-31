using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;
using TaskPulse.API.Authentication;
using TaskPulse.API.Data;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Helper.ValidationHelper.EntityExistenceValidationHelper;
using TaskPulse.API.Repository.ActivityLogRepo;
using TaskPulse.API.Repository.UserRepo;
using TaskPulse.API.Services.implementations;
using TaskPulse.API.Services.interfaces;
using TaskPulse.API.Validation;
using TaskPulse.API.Validation.UserValidation;

var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly, includeInternalTypes: true);
builder.Services.AddScoped<EntityExistenceChecker>();
builder.Services.AddScoped<IUserServices, UserService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<CustomExceptionHandler>();
builder.Services.AddScoped<CheckValidationRun>();
builder.Services.AddScoped<IValidation<CreateUserDto>, CreateUserValidation>();
builder.Services.AddScoped<IValidation<LoginDto>, LoginValidation>();
builder.Services.AddScoped<IValidation<RefreshTokenRequestDto>, RefreshTokenValidation>();
builder.Services.AddScoped<IValidation<ConfirmEmailDto>, ConfirmEmailValidation>();
builder.Services.AddScoped<IValidation<ResendConfirmationDto>, ResendConfirmationValidation>();
builder.Services.AddScoped<IValidation<ForgotPasswordDto>, ForgotPasswordValidation>();
builder.Services.AddScoped<IValidation<ResetPasswordDto>, ResetPasswordValidation>();
builder.Services.AddScoped<IValidation<UpdateUserProfileDto>, UpdateUserProfileValidation>();
builder.Services.AddScoped<IValidation<UpdateUserRoleDto>, UpdateUserRoleValidation>();
builder.Services.AddScoped<IValidation<UpdateUserStatusDto>, UpdateUserStatusValidation>();
builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();
builder.Services.AddScoped<IAccountTokenNotifier, AccountTokenNotifier>();
builder.Services.AddScoped<ActivityLogRepository>();
builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

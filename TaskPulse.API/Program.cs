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
using TaskPulse.API.DTO.ProjectDto;
using TaskPulse.API.DTO.ProjectMemberDto;
using TaskPulse.API.DTO.TaskDto;
using TaskPulse.API.Helper.FileHelper;
using TaskPulse.API.Helper.ValidationHelper.EntityExistenceValidationHelper;
using TaskPulse.API.Repository.ActivityLogRepo;
using TaskPulse.API.Repository.ApprovalRepo;
using TaskPulse.API.Repository.CommentRepo;
using TaskPulse.API.Repository.DashboardRepo;
using TaskPulse.API.Repository.NotificationRepo;
using TaskPulse.API.Repository.ProjectRepo;
using TaskPulse.API.Repository.TaskApprovalRepo;
using TaskPulse.API.Repository.TaskRepo;
using TaskPulse.API.Repository.UserRepo;
using TaskPulse.API.Services.BackgroundServices;
using TaskPulse.API.Services.implementations.ApprovalHandlers;
using TaskPulse.API.Validation.ProjectValidation;
using TaskPulse.API.Validation.TaskValidation;
using TaskPulse.API.Services.implementations;
using TaskPulse.API.Services.interfaces;
using TaskPulse.API.Validation;
using TaskPulse.API.Validation.UserValidation;

// Must exist before WebApplication.CreateBuilder runs — that's where the
// static file provider binds to wwwroot, and it falls back to a null
// provider for the app's whole lifetime if the folder isn't there yet.
Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars"));
Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "chat-images"));

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

// The frontend dev server runs on its own origin — without this, every
// request from it is blocked by the browser before it reaches here.
const string AngularDevClient = "AngularDevClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevClient, policy =>
    {
        policy.WithOrigins(
                  "http://localhost:4200", "https://localhost:4200",
                  "http://localhost:3000", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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
builder.Services.AddScoped<IValidation<ChangePasswordDto>, ChangePasswordValidation>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IAccountTokenNotifier, AccountTokenNotifier>();
builder.Services.AddScoped<IValidation<CreateProjectDto>, CreateProjectValidation>();
builder.Services.AddScoped<IValidation<UpdateProjectDto>, UpdateProjectValidation>();
builder.Services.AddScoped<IValidation<AddProjectMemberDto>, AddProjectMemberValidation>();
builder.Services.AddScoped<ActivityLogRepository>();
builder.Services.AddScoped<NotificationRepository>();
builder.Services.AddScoped<ProjectRepository>();
builder.Services.AddScoped<ApprovalRequestRepository>();
builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
builder.Services.AddScoped<INotifier, Notifier>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IApprovalGate, ApprovalGate>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();

// One handler per gated action. ApprovalService receives them all and picks by
// ActionType — a new gated action means a new handler plus a line here.
builder.Services.AddScoped<IApprovalActionHandler, UpdateProjectApprovalHandler>();
builder.Services.AddScoped<IApprovalActionHandler, DeleteProjectApprovalHandler>();
builder.Services.AddScoped<IApprovalActionHandler, RemoveProjectMemberApprovalHandler>();
builder.Services.AddScoped<IApprovalActionHandler, CompleteTaskApprovalHandler>();
builder.Services.AddScoped<IApprovalActionHandler, AssignTaskRequestApprovalHandler>();
builder.Services.AddScoped<IApprovalActionHandler, UpdateTaskApprovalHandler>();
builder.Services.AddScoped<IApprovalActionHandler, AssignBusyUserApprovalHandler>();
builder.Services.AddScoped<IApprovalActionHandler, DeleteTaskApprovalHandler>();
builder.Services.AddScoped<IApprovalActionHandler, DuplicateTaskApprovalHandler>();

builder.Services.AddScoped<IValidation<CreateTaskDto>, CreateTaskValidation>();
builder.Services.AddScoped<IValidation<UpdateTaskDto>, UpdateTaskValidation>();
builder.Services.AddScoped<TaskRepository>();
builder.Services.AddScoped<TaskApprovalRequestRepository>();
builder.Services.AddScoped<CommentRepository>();
builder.Services.AddScoped<DashboardRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ITaskFileService, TaskFileService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// AI features (project summary, risk detection, in-app help) all go through
// Gemini via this one typed HttpClient.
builder.Services.AddHttpClient<GeminiClient>();
builder.Services.AddScoped<IAiInsightsService, AiInsightsService>();
builder.Services.AddScoped<IHelpAssistantService, HelpAssistantService>();

// File upload limits and storage. Singleton because the options are read once
// from configuration and neither class holds per-request state.
builder.Services.AddSingleton<FileUploadOptions>();
builder.Services.AddSingleton<UploadedFileValidator>();
builder.Services.AddSingleton<FileStorage>();

// Sends the "due soon" and "deadline passed" notifications. Nothing else in the
// app notices time passing on its own.
builder.Services.AddHostedService<TaskDeadlineMonitor>();
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
app.UseCors(AngularDevClient);
// Avatars are served straight off disk — low-sensitivity, so no auth/controller
// needed for a plain <img src> to work.
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Top-level statements compile into an internal Program class, which the test
// project can't reach. This makes it public so WebApplicationFactory<Program>
// can boot the real app in memory. It adds nothing at runtime.
public partial class Program { }

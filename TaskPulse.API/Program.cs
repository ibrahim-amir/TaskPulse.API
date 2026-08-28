using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using TaskPulse.API.Data;
using TaskPulse.API.DTO.UserDto;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;
using TaskPulse.API.Helper.ValidationHelper.EntityExistenceValidationHelper;
using TaskPulse.API.Repository.implementations;
using TaskPulse.API.Repository.interfaces;
using TaskPulse.API.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<EntityExistenceChecker>();
builder.Services.AddScoped<IUserServices, UserService>();
builder.Services.AddScoped<CustomExceptionHandler>();
builder.Services.AddScoped<CheckValidationRun>();
builder.Services.AddScoped<IValidation<CreateUserDto>, CreateUserValidation>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

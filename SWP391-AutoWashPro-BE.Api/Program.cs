using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Api.Extensions;
using SWP391_AutoWashPro_BE.Api.Extentions;
using SWP391_AutoWashPro_BE.Api.Middlewares;
using SWP391_AutoWashPro_BE.Repository;
using JwtService = SWP391_AutoWashPro_BE.Service.JwtService;
using CloudinaryService = SWP391_AutoWashPro_BE.Service.CloudinaryService;
using MediaService = SWP391_AutoWashPro_BE.Service.MediaService;
using MailService = SWP391_AutoWashPro_BE.Service.MailService;
using UserService = SWP391_AutoWashPro_BE.Service.User;
using AdminService = SWP391_AutoWashPro_BE.Service.Admin;
using SecurityService = SWP391_AutoWashPro_BE.Service.Security;



var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddJwtServices(builder.Configuration);
builder.Services.AddSwaggerServices();

builder.Services.AddScoped<JwtService.IService, JwtService.Service>();
builder.Services.AddScoped<MediaService.IService, CloudinaryService.Service>();
builder.Services.AddScoped<MailService.IService, MailService.Service>();
builder.Services.AddScoped<SecurityService.IService, SecurityService.Service>();
builder.Services.AddScoped<UserService.IService, UserService.Service>();
builder.Services.AddScoped<AdminService.IService, AdminService.Service>();

builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerAPI();
}

//Testing

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

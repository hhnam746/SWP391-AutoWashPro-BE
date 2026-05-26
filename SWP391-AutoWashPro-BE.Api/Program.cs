using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Api.Extensions;
using SWP391_AutoWashPro_BE.Api.Extentions;
using SWP391_AutoWashPro_BE.Api.Middlewares;
using SWP391_AutoWashPro_BE.Repository;
using JwtService = SWP391_AutoWashPro_BE.Service.JwtService;
using CloudinaryService = SWP391_AutoWashPro_BE.Service.CloudinaryService;
using MediaService = SWP391_AutoWashPro_BE.Service.MediaService;
using MailService = SWP391_AutoWashPro_BE.Service.MailService;
using AuthService = SWP391_AutoWashPro_BE.Service.Auth;
using UserService = SWP391_AutoWashPro_BE.Service.User;
using AdminService = SWP391_AutoWashPro_BE.Service.Admin;
using SecurityService = SWP391_AutoWashPro_BE.Service.Security;
using System.Text.Json.Serialization;

using VehicleService = SWP391_AutoWashPro_BE.Service.Vehicles;
using WalletService = SWP391_AutoWashPro_BE.Service.Wallet;
using NotificationService = SWP391_AutoWashPro_BE.Service.Notification;
using BranchAndTierService = SWP391_AutoWashPro_BE.Service.Branch;
using BookingService = SWP391_AutoWashPro_BE.Service.Booking;
using LoyaltyService = SWP391_AutoWashPro_BE.Service.Loyalty;
var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
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
builder.Services.AddScoped<AuthService.IService, AuthService.Service>();
builder.Services.AddScoped<AdminService.IService, AdminService.Service>();
builder.Services.AddScoped<VehicleService.IService, VehicleService.Service>();
builder.Services.AddScoped<WalletService.IService, WalletService.Service>();
builder.Services.AddScoped<NotificationService.IService, NotificationService.Service>();
builder.Services.AddScoped<BranchAndTierService.IService, BranchAndTierService.Service>();
builder.Services.AddScoped<BookingService.IService, BookingService.Service>();
builder.Services.AddScoped<LoyaltyService.IService, LoyaltyService.Service>();
builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerAPI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

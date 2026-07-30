using Microsoft.EntityFrameworkCore;
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
using DotNetEnv;
using Quartz;
using StackExchange.Redis;
using SWP391_AutoWashPro_BE.Service.BackgroundJob;
// using SWP391_AutoWashPro_BE.Service.BackgroundJob;
using VehicleService = SWP391_AutoWashPro_BE.Service.Vehicles;
using WalletService = SWP391_AutoWashPro_BE.Service.Wallet;
using NotificationService = SWP391_AutoWashPro_BE.Service.Notification;
using BranchAndTierService = SWP391_AutoWashPro_BE.Service.Branch;
using BookingService = SWP391_AutoWashPro_BE.Service.Booking;
using LoyaltyService = SWP391_AutoWashPro_BE.Service.Loyalty;
using TierService = SWP391_AutoWashPro_BE.Service.Tier;
using PromotionService = SWP391_AutoWashPro_BE.Service.Promotion;
using RewardService = SWP391_AutoWashPro_BE.Service.Reward;
using VoucherService = SWP391_AutoWashPro_BE.Service.Voucher;
using DiscordService = SWP391_AutoWashPro_BE.Service.DiscordService;
using NotificationHub = SWP391_AutoWashPro_BE.Service.Hubs.NotificationHub;
using OtpService = SWP391_AutoWashPro_BE.Service.OtpService;
using RedisOtpService = SWP391_AutoWashPro_BE.Service.RedisOtpService;
using Transaction =  SWP391_AutoWashPro_BE.Service.Transaction;
using AiService = SWP391_AutoWashPro_BE.Service.AiService;
using PersonalizedVoucherService = SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

Env.Load();

var aspnetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", aspnetCoreEnv);

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

builder.Services.ConfigureRateLimiter();
builder.Services.AddJwtServices(builder.Configuration);
builder.Services.AddSwaggerServices();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173",  // port của FE và BE
                    "http://localhost:5174", //local demo SignalR
                    "http://localhost:3000",  //local demo FE
                    "http://localhost:5207",
                    "https://auto-wash-pro.vercel.app" //apply vercel deploy FE
                    )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); 
        });
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connection = builder.Configuration["Redis:ConnectionString"];
    return ConnectionMultiplexer.Connect(connection!);
});

builder.Services.AddScoped<SWP391_AutoWashPro_BE.Service.OTPDemoService.OtpService>();
builder.Services.AddScoped<RedisOtpService.IService, RedisOtpService.Service>();

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
builder.Services.AddScoped<TierService.IService, TierService.Service>();
builder.Services.AddScoped<PromotionService.IService, PromotionService.Service>();
builder.Services.AddScoped<RewardService.IService, RewardService.Service>();
builder.Services.AddScoped<VoucherService.IService, VoucherService.Service>();
builder.Services.AddScoped<OtpService.IService, OtpService.Service>();
builder.Services.AddScoped<Transaction.IService, Transaction.Service>();
builder.Services.AddScoped<AiService.IService, AiService.Service>();
builder.Services.AddScoped<AiService.IntentDetector>();
builder.Services.AddScoped<AiService.PromptBuilder>();
builder.Services.AddHttpClient<AiService.GoogleAiStudioService>();
builder.Services.Configure<PersonalizedVoucherService.Options>(
    builder.Configuration.GetSection(PersonalizedVoucherService.Options.SectionName));
builder.Services.AddScoped<PersonalizedVoucherService.IService, PersonalizedVoucherService.Service>();
builder.Services.AddScoped<PersonalizedVoucherService.IRuleService, PersonalizedVoucherService.RuleService>();
builder.Services.AddScoped<PersonalizedVoucherService.IDeliveryService, PersonalizedVoucherService.DeliveryService>();
builder.Services.AddScoped<PersonalizedVoucherService.ITriggerConfigService,
    PersonalizedVoucherService.TriggerConfigService>();
builder.Services.AddScoped<PersonalizedVoucherService.IAudienceService, PersonalizedVoucherService.AudienceService>();

//test thử discord
builder.Services.Configure<DiscordService.DiscordAlertOptions>(
    builder.Configuration.GetSection("DiscordAlertOptions"));
builder.Services.AddHttpClient<DiscordService.IService, DiscordService.Service>(); // AddHttpClient là do nó tự gọi API ở bên ngoài
// Cụ thể ở đây của mình là tự gọi API webhook của discord

//backgroundJob | Cron job
var processBookingCron = builder.Configuration["Quartz:ProcessBookingCron"] ?? "0/15 * * * * ?";
var processBookingReminderCron = builder.Configuration["Quartz:BookingReminderCron"] ?? "0/30 * * * * ?"; //30s quét 1 lần
var processBookingCompletedCron = builder.Configuration["Quartz:BookingCompletedCron"] ?? "0/30 * * * * ?";

var defaultBirthdayVoucherCron = "0/5 * * * * ?"; // 5 giây/lần

var birthdayVoucherCron =
    builder.Configuration["Quartz:BirthdayVoucherCron"]
    ?? defaultBirthdayVoucherCron;
// var birthdayVoucherCron = builder.Configuration["Quartz:BirthdayVoucherCron"] ?? "0/5 * * * * ?";
var inactiveCustomerVoucherCron = builder.Configuration["Quartz:InactiveCustomerVoucherCron"] ?? "0 15 1 * * ?";
var acquisitionVoucherCron = builder.Configuration["Quartz:AcquisitionVoucherCron"] ?? "0 0/15 * * * ?";
var personalizedVoucherDeliveryRetryCron =
    builder.Configuration["Quartz:PersonalizedVoucherDeliveryRetryCron"] ?? "0 0/10 * * * ?";
var personalizedVoucherTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
    builder.Configuration[$"{PersonalizedVoucherService.Options.SectionName}:TimeZoneId"] ??
    "Asia/Ho_Chi_Minh");
builder.Services.AddQuartz(options =>
{
    var processBookingJobKey = new JobKey(nameof(ProcessBookingAutoCancelJob));
    var processBookingReminderJobKey = new JobKey(nameof(ProcessBookingReminderJob));
    var processBookingCompletedJobKey = new JobKey(nameof(ProcessBookingAutoCompleteJob));
    var birthdayVoucherJobKey = new JobKey(nameof(ProcessBirthdayVoucherJob));
    var inactiveCustomerVoucherJobKey = new JobKey(nameof(ProcessInactiveCustomerVoucherJob));
    var acquisitionVoucherJobKey = new JobKey(nameof(ProcessAcquisitionVoucherJob));
    var personalizedVoucherDeliveryRetryJobKey = new JobKey(nameof(RetryPersonalizedVoucherDeliveryJob));

    options.AddJob<ProcessBookingAutoCancelJob>(job => job
        .WithIdentity(processBookingJobKey));

    options.AddJob<ProcessBookingReminderJob>(job => job
        .WithIdentity(processBookingReminderJobKey));

    options.AddJob<ProcessBookingAutoCompleteJob>(job => job
        .WithIdentity(processBookingCompletedJobKey));

    options.AddJob<ProcessBirthdayVoucherJob>(job => job.WithIdentity(birthdayVoucherJobKey));
    options.AddJob<ProcessInactiveCustomerVoucherJob>(job => job.WithIdentity(inactiveCustomerVoucherJobKey));
    options.AddJob<ProcessAcquisitionVoucherJob>(job => job.WithIdentity(acquisitionVoucherJobKey));
    options.AddJob<RetryPersonalizedVoucherDeliveryJob>(job =>
        job.WithIdentity(personalizedVoucherDeliveryRetryJobKey));

    options.AddTrigger(trigger => trigger
        .ForJob(processBookingJobKey)
        .WithIdentity($"{nameof(ProcessBookingAutoCancelJob)}-trigger")
        // Mặc định chạy mỗi 15 giây để test gần realtime, có thể override bằng Quartz:ProcessBookingCron.
        .WithCronSchedule(processBookingCron, cron =>
            cron.WithMisfireHandlingInstructionDoNothing()));

    options.AddTrigger(trigger => trigger
        .ForJob(processBookingReminderJobKey)
        .WithIdentity($"{nameof(ProcessBookingReminderJob)}-trigger")
        .WithCronSchedule(processBookingReminderCron, cron =>
            cron.WithMisfireHandlingInstructionDoNothing()));

    options.AddTrigger(trigger => trigger
        .ForJob(processBookingCompletedJobKey)
        .WithIdentity($"{nameof(ProcessBookingAutoCompleteJob)}-trigger")
        .WithCronSchedule(processBookingCompletedCron, cron =>
            cron.WithMisfireHandlingInstructionDoNothing()));

    options.AddTrigger(trigger => trigger
        .ForJob(birthdayVoucherJobKey)
        .WithIdentity($"{nameof(ProcessBirthdayVoucherJob)}-trigger")
        .WithCronSchedule(birthdayVoucherCron, cron => cron
            .InTimeZone(personalizedVoucherTimeZone)
            .WithMisfireHandlingInstructionDoNothing()));

    options.AddTrigger(trigger => trigger
        .ForJob(inactiveCustomerVoucherJobKey)
        .WithIdentity($"{nameof(ProcessInactiveCustomerVoucherJob)}-trigger")
        .WithCronSchedule(inactiveCustomerVoucherCron, cron => cron
            .InTimeZone(personalizedVoucherTimeZone)
            .WithMisfireHandlingInstructionDoNothing()));

    options.AddTrigger(trigger => trigger
        .ForJob(acquisitionVoucherJobKey)
        .WithIdentity($"{nameof(ProcessAcquisitionVoucherJob)}-trigger")
        .WithCronSchedule(acquisitionVoucherCron, cron => cron
            .InTimeZone(personalizedVoucherTimeZone)
            .WithMisfireHandlingInstructionDoNothing()));

    options.AddTrigger(trigger => trigger
        .ForJob(personalizedVoucherDeliveryRetryJobKey)
        .WithIdentity($"{nameof(RetryPersonalizedVoucherDeliveryJob)}-trigger")
        .WithCronSchedule(personalizedVoucherDeliveryRetryCron, cron => cron
            .InTimeZone(personalizedVoucherTimeZone)
            .WithMisfireHandlingInstructionDoNothing()));
});


builder.Services.AddQuartzHostedService(options =>
{
    //// Auto-cancel được xử lý tập trung bởi Quartz job ProcessBookingJob.
    options.WaitForJobsToComplete = true;
});

builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwaggerAPI();
// }

//Testing

app.UseSwaggerAPI();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notificationHub");
app.MapControllers();

app.Run();

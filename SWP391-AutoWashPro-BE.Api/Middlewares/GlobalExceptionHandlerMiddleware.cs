using SWP391_AutoWashPro_BE.Service.DiscordService;
using SWP391_AutoWashPro_BE.Service.Models;

namespace SWP391_AutoWashPro_BE.Api.Middlewares;

public class GlobalExceptionHandlerMiddleware : IMiddleware
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IService _discordService; 

    public GlobalExceptionHandlerMiddleware(
        IHostEnvironment environment,
        ILogger<GlobalExceptionHandlerMiddleware> logger, IService discordService)
    {
        _environment = environment;
        _logger = logger;
        _discordService = discordService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            if (ex is BookingScheduleWarningException scheduleWarning)
            {
                _logger.LogWarning(
                    "Booking schedule warning returned for request {Path}. ConflictCount={ConflictCount}.",
                    context.Request.Path,
                    scheduleWarning.Warning.Conflicts.Count);
            }
            else
            {
                _logger.LogError(ex, "Unhandled exception occurred while processing request {Path}", context.Request.Path);
            }

            if (context.Response.HasStarted)
            {
                // Lỗi nâng cao anh sẽ nói sau
                _logger.LogWarning("The response has already started, the global exception middleware will not write an error response");
                throw;
            }

            var statusCode = MapStatusCode(ex);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            // chỉ gửi Discord khi là lỗi 500
            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                await _discordService.SendExceptionAlertAsync(context, ex, statusCode);
            }
            
            var response = ApiResponseFactory.ErrorResponse(
                message: ResolveClientMessage(ex, statusCode),
                errors: ResolveErrors(ex),
                traceId: context.TraceIdentifier);

            await context.Response.WriteAsJsonAsync(response);
        }
    }

    private static int MapStatusCode(Exception ex)
    {
        return ex switch
        {
            BookingScheduleWarningException => StatusCodes.Status409Conflict,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ForbiddenAccessException => StatusCodes.Status403Forbidden,
            TooManyRequestsException => StatusCodes.Status429TooManyRequests,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string ResolveClientMessage(Exception ex, int statusCode)
    {
        return statusCode >= 500 ? "An unexpected error occurred" : ex.Message;
    }

    private object? ResolveErrors(Exception ex)
    {
        if (ex is BookingScheduleWarningException scheduleWarning)
        {
            return scheduleWarning.Warning;
        }

        return _environment.IsDevelopment() ? new { detail = ex.Message } : null;
    }
}

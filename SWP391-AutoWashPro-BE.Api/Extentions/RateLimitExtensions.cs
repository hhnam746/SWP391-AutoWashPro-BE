using System.Threading.RateLimiting;

namespace SWP391_AutoWashPro_BE.Api.Extentions;

public static class RateLimitExtensions
{
    public static void ConfigureRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.Headers["Retry-After"] = "60";
                await context.HttpContext.Response
                    .WriteAsync("Too many requests. Please retry later.", cancellationToken: _);
            };

            // Global rate limiter applied to all endpoints
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    context.User.Identity?.Name
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 250,
                        Window = TimeSpan.FromSeconds(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.AddPolicy("api", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    context.User.Identity?.Name
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // fail fast
                    }));

            // Strict login policy
            options.AddPolicy("login", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // Heavy operation limit
            options.AddPolicy("heavy", context =>
                RateLimitPartition.GetTokenBucketLimiter(
                    context.User.Identity?.Name ?? "anonymous",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 10,
                        TokensPerPeriod = 5,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });
    }
}
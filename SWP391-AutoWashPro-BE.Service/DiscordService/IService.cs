using Microsoft.AspNetCore.Http;

namespace SWP391_AutoWashPro_BE.Service.DiscordService;

public interface IService
{
    Task SendExceptionAlertAsync(
        HttpContext context,
        Exception exception,
        int statusCode,
        CancellationToken cancellationToken = default);
}
using Microsoft.Extensions.Logging;
using Quartz;
using SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

namespace SWP391_AutoWashPro_BE.Service.BackgroundJob;

[DisallowConcurrentExecution]
public class RetryPersonalizedVoucherDeliveryJob : IJob
{
    private readonly IDeliveryService _deliveryService;
    private readonly ILogger<RetryPersonalizedVoucherDeliveryJob> _logger;

    public RetryPersonalizedVoucherDeliveryJob(
        IDeliveryService deliveryService,
        ILogger<RetryPersonalizedVoucherDeliveryJob> logger)
    {
        _deliveryService = deliveryService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var processed = await _deliveryService.RetryPendingAsync(context.CancellationToken);
        _logger.LogInformation(
            "Personalized voucher delivery retry batch completed. ProcessedCount={ProcessedCount}, StartedAt={StartedAt}, CompletedAt={CompletedAt}.",
            processed,
            startedAt,
            DateTimeOffset.UtcNow);
    }
}

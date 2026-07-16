using Microsoft.Extensions.Logging;
using Quartz;
using SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

namespace SWP391_AutoWashPro_BE.Service.BackgroundJob;

[DisallowConcurrentExecution]
public class ProcessInactiveCustomerVoucherJob : IJob
{
    private readonly IAudienceService _audienceService;
    private readonly ILogger<ProcessInactiveCustomerVoucherJob> _logger;

    public ProcessInactiveCustomerVoucherJob(
        IAudienceService audienceService,
        ILogger<ProcessInactiveCustomerVoucherJob> logger)
    {
        _audienceService = audienceService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var processed = await _audienceService.ProcessInactiveCustomersAsync(context.CancellationToken);
        _logger.LogInformation(
            "Inactive customer voucher batch completed. ProcessedCount={ProcessedCount}, StartedAt={StartedAt}, CompletedAt={CompletedAt}.",
            processed,
            startedAt,
            DateTimeOffset.UtcNow);
    }
}

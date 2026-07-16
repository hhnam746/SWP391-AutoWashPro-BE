using Microsoft.Extensions.Logging;
using Quartz;
using SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

namespace SWP391_AutoWashPro_BE.Service.BackgroundJob;

[DisallowConcurrentExecution]
public class ProcessBirthdayVoucherJob : IJob
{
    private readonly IAudienceService _audienceService;
    private readonly ILogger<ProcessBirthdayVoucherJob> _logger;

    public ProcessBirthdayVoucherJob(
        IAudienceService audienceService,
        ILogger<ProcessBirthdayVoucherJob> logger)
    {
        _audienceService = audienceService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var processed = await _audienceService.ProcessBirthdayAsync(context.CancellationToken);
        _logger.LogInformation(
            "Birthday voucher batch completed. ProcessedCount={ProcessedCount}, StartedAt={StartedAt}, CompletedAt={CompletedAt}.",
            processed,
            startedAt,
            DateTimeOffset.UtcNow);
    }
}

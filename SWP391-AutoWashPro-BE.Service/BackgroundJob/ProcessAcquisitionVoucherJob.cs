using Microsoft.Extensions.Logging;
using Quartz;
using SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

namespace SWP391_AutoWashPro_BE.Service.BackgroundJob;

[DisallowConcurrentExecution]
public class ProcessAcquisitionVoucherJob : IJob
{
    private readonly IAudienceService _audienceService;
    private readonly ILogger<ProcessAcquisitionVoucherJob> _logger;

    public ProcessAcquisitionVoucherJob(
        IAudienceService audienceService,
        ILogger<ProcessAcquisitionVoucherJob> logger)
    {
        _audienceService = audienceService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var processed = await _audienceService.ProcessAcquisitionAsync(context.CancellationToken);
        _logger.LogInformation(
            "Acquisition voucher batch completed. ProcessedCount={ProcessedCount}, StartedAt={StartedAt}, CompletedAt={CompletedAt}.",
            processed,
            startedAt,
            DateTimeOffset.UtcNow);
    }
}

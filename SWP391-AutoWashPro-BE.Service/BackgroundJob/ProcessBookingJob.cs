using Quartz;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.BackgroundJob;

[DisallowConcurrentExecution]
public class ProcessBookingJob : IJob
{
    private static readonly TimeSpan DefaultPendingTimeout = TimeSpan.FromMinutes(5);
    
    public Task Execute(IJobExecutionContext context)
    {
        throw new NotImplementedException();
    }
}
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public interface ITriggerConfigService
{
    Task<bool> IsEnabledAsync(
        PersonalizedVoucherTriggerType triggerType,
        CancellationToken cancellationToken = default);
}

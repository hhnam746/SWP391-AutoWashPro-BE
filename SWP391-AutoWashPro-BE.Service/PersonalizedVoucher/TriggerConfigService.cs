using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Constants;
using SWP391_AutoWashPro_BE.Repository.DbContext;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public class TriggerConfigService : ITriggerConfigService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TriggerConfigService> _logger;

    public TriggerConfigService(
        AppDbContext dbContext,
        ILogger<TriggerConfigService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(
        PersonalizedVoucherTriggerType triggerType,
        CancellationToken cancellationToken = default)
    {
        var configKey = PersonalizedVoucherConfigKeys.ForTrigger(triggerType);
        var configValue = await _dbContext.SystemConfigs
            .AsNoTracking()
            .Where(x => x.ConfigKey == configKey)
            .Select(x => x.ConfigValue)
            .FirstOrDefaultAsync(cancellationToken);

        if (configValue == null)
        {
            _logger.LogWarning(
                "Personalized voucher trigger disabled because SystemConfig is missing. TriggerType={TriggerType}, ConfigKey={ConfigKey}.",
                triggerType,
                configKey);
            return false;
        }

        if (!bool.TryParse(configValue, out var isEnabled))
        {
            _logger.LogWarning(
                "Personalized voucher trigger disabled because SystemConfig is invalid. TriggerType={TriggerType}, ConfigKey={ConfigKey}.",
                triggerType,
                configKey);
            return false;
        }

        return isEnabled;
    }
}

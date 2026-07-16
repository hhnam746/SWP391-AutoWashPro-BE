using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public interface IService
{
    Task<Response.IssueResult> TryIssuePersonalizedVoucherAsync(
        Guid customerId,
        Guid promotionRuleId,
        PersonalizedVoucherTriggerType triggerType,
        string cycleKey,
        string? triggerReference,
        CancellationToken cancellationToken = default);
}

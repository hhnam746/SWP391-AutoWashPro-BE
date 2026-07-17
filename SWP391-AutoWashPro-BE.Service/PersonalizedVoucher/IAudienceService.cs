namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public interface IAudienceService
{
    Task<int> ProcessBirthdayAsync(CancellationToken cancellationToken = default);
    Task<int> ProcessInactiveCustomersAsync(CancellationToken cancellationToken = default);
    Task<int> ProcessAcquisitionAsync(CancellationToken cancellationToken = default);
    Task<Response.IssueResult> ProcessTierUpgradeAsync(
        Guid customerId,
        Guid newTierId,
        Guid bookingId,
        CancellationToken cancellationToken = default);
}

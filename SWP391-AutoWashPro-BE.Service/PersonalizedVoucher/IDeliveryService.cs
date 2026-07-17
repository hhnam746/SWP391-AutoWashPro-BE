namespace SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

public interface IDeliveryService
{
    Task DispatchAsync(Guid issuanceId, CancellationToken cancellationToken = default);
    Task<int> RetryPendingAsync(CancellationToken cancellationToken = default);
}

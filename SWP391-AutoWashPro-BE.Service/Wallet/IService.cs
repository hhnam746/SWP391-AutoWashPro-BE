namespace SWP391_AutoWashPro_BE.Service.Wallet;

public interface IService
{
    public Task<Response.GetWalleResponse> GetUserWallet();
    public Task<Response.WalletTopupResponse> TopupUserWallet(Request.WalletTopupRequest request);
    public Task<Response.GetWalleResponse> GetUserWalletV2();
    public Task<Response.WalletTopupV2Response> TopupUserWalletV2(Request.WalletTopupRequest request);
    public Task<Response.WalletTopupStatusResponse> GetWalletTopupStatus(
        Guid transactionId,
        CancellationToken cancellationToken = default);
}

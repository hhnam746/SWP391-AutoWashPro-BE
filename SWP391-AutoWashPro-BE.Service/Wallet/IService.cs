namespace SWP391_AutoWashPro_BE.Service.Wallet;

public interface IService
{
    public Task<Response.GetWalleResponse> GetUserWallet();
    public Task<Response.WalletTopupResponse> TopupUserWallet(Request.WalletTopupRequest request);
}
namespace SWP391_AutoWashPro_BE.Service.Loyalty;

public interface IService
{
    public Task<Response.LoyaltyMeResponse> GetMyLoyaltyOverview();
    Task<Response.GetPointTransactionsResponse> GetPointTransactions(Request.GetPointTransactionsRequest request);
}
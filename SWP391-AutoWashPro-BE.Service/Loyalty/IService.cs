namespace SWP391_AutoWashPro_BE.Service.Loyalty;

public interface IService
{
    public Task<Response.LoyaltyMeResponse> GetMyLoyaltyOverview();
    Task<Response.GetPointTransactionsResponse> GetPointTransactions(Request.GetPointTransactionsRequest request);
    
    public Task<List<Response.ConfigResponse>> GetAllConfigs();
    public Task<string> UpdateConfig(Request.ConfigRequest request);
}
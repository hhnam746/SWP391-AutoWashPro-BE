namespace SWP391_AutoWashPro_BE.Service.Reward;

public interface IService
{
    public Task<Base.Response.PageResult<Response.RewardResponse>> GetAllReward(string? searchTerm, int pageSize,
        int pageIndex);
    public Task<string> CreateReward(Request.RewardRequest request);

    public Task<string> UpdateReward(Guid id, Request.RewardRequest request);
    
    public Task<string> DeleteReward(Guid id);
    
    public Task<string> RedeemReward(Guid rewardId);
}
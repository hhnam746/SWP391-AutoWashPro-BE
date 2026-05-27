namespace SWP391_AutoWashPro_BE.Service.Branch;

public interface IService
{
    public Task<Response.GetBranchesResponse> GetBranches(string? keyword, bool? IsActive);
    public Task<Response.GetTiersResponse> GetTiers();
    public Task<Response.GetUserAvailablePromotion> GetPromotions();
    public Task<Response.GetRewardsResponse> GetRewards();
    
    public Task<Base.Response.PageResult<Response.BranchItem>> GetAllBranches(string? searchTerm, int pageSize, int pageIndex);
    public Task<string> CreateBranch(Request.BranchRequest request);
    public Task<string> UpdateBranch(Guid id, Request.BranchRequest request);
    
    public Task<string> DeleteBranch(Guid id);
}
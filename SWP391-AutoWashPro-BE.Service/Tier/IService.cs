namespace SWP391_AutoWashPro_BE.Service.Tier;

public interface IService
{
    public Task<Base.Response.PageResult<Response.TierResponse>> GetAllTier(string? searchTerm, int pageSize, int pageIndex);
    public Task<string> CreateTier(Request.TierRequest request);
    public Task<string> UpdateTier(Guid id, Request.TierRequest request);
    public Task<string> DeleteTier(Guid id);
}
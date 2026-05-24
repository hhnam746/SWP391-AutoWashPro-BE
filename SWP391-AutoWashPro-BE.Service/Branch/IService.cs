namespace SWP391_AutoWashPro_BE.Service.Branch;

public interface IService
{
    public Task<Response.GetBranchesResponse> GetBranches(string? keyword, bool? IsActive);
    public Task<Response.GetTiersResponse> GetTiers();
}
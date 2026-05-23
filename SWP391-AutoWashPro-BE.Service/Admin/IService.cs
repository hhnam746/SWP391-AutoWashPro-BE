namespace SWP391_AutoWashPro_BE.Service.Admin;

public interface IService
{
    public Task<string> UpdateUserVerificationStatus(Guid userId);
    public Task<Base.Response.PageResult<Response.AllProfileResponse>> GetAllUserProfile(
        string? searchTerm,
        int pageSize,
        int pageIndex);
    public Task<Base.Response.PageResult<Response.AllProfileResponse>> GetUsersNeedVerification(
        string? searchTerm,
        int pageSize,
        int pageIndex);
    public Task<Response.GetUserByIdResponse> GetUserById(Guid userId);
    public Task<Response.GetUserStatusResponse> GetUserStatusById(Guid userId);
    public Task<string> UpdateUserStatusById(Guid userId, Request.UpdateUserByStatusRequest request);
}

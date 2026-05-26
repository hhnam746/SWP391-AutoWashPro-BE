namespace SWP391_AutoWashPro_BE.Service.User;

public interface IService
{
    public Task<Response.ProfileResponse> GetProfile();
    public Task<string> UpdateProfile(Request.UpdateProfileRequest request);
    public Task<string> UpdateProfileByPassword(Request.UpdateProfileByPassword request);
}
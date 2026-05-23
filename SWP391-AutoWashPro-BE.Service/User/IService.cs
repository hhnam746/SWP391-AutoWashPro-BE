namespace SWP391_AutoWashPro_BE.Service.User;

public interface IService
{
    public Task<Response.ProfileResponse> GetProfile();
}
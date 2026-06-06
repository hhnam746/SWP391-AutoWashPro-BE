namespace SWP391_AutoWashPro_BE.Service.User;

public interface IService
{
    public Task<Response.ProfileResponse> GetProfile();
    public Task<string> UpdateProfile(Request.UpdateProfileRequest request);
    public Task<string> ChangePasswordRequest(Request.ChangePasswordRequest request);

    public Task<Response.GetMyStatus> GetMyStatus();
    public Task<string> ResubmitVerification(Request.VerificationResubmissionRequest request);
}
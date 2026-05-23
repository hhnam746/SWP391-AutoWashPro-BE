namespace SWP391_AutoWashPro_BE.Service.Auth;

public interface IService
{
    public Task<string> Register(Request.RegisterRequest request);
    public Task<Response.LoginResponse> Login(Request.LoginRequest request);
}
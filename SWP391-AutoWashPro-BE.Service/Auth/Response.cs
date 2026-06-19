namespace SWP391_AutoWashPro_BE.Service.Auth;

public class Response
{
    public class LoginResponse
    {
        public string Access_token { get; set; } = null!;
        public bool isVerify { get; set; }
    }
}
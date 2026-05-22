namespace SWP391_AutoWashPro_BE.Service.User;

public class Response
{
    public class LoginResponse
    {
        public string Access_token { get; set; } = null!;
        public bool isVerify { get; set; }
    }
}
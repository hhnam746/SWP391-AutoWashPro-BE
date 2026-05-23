namespace SWP391_AutoWashPro_BE.Service.User;

public class Request
{
    public class UpdateProfileRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Cccd { get; set; }
    }

    public class UpdateProfileByPassword
    {
        public string? NewPassword { get; set; }
    }
}

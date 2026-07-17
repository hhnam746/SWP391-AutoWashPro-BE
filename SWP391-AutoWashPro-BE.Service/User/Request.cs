using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SWP391_AutoWashPro_BE.Service.User;

public class Request
{
    public class UpdateProfileRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone {  get; set; }
        public DateOnly? DateOfBirth { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
    
    public class VerificationResubmissionRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        // public string? Cccd { get; set; }
        
        [MinLength(3, ErrorMessage = "At least 3 face images are required.")]
        public List<IFormFile>? FaceImages { get; set; } = new();
    }
    
    
}

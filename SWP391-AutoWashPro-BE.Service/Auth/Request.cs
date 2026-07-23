using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SWP391_AutoWashPro_BE.Service.Auth;

public class Request
{
    public class RegisterRequest
    {
        public required string Email { get; set; }
        
        public required string Phone { get; set; }
        
        public required string Password { get; set; }
        
        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        public DateOnly? DateOfBirth { get; set; }
        
        [MinLength(3, ErrorMessage = "At least 3 face images are required.")]
        public List<IFormFile> FaceImages { get; set; } = new();
    }
    
    public class LoginRequest
    {
        public required string Identifier { get; set; }
        public required string Password { get; set; }
    }
    
    // For OTP
    
    public class ForgotPasswordRequest
    {
        public required string Email { get; set; }
    }

    public class VerifyOtpRequest
    {
        public required string Email { get; set; }
        public required string Otp { get; set; }
    }
    
    public class ResetPasswordRequest
    {
        public required string ResetPasswordToken { get; set; }
        public required string NewPassword { get; set; }
        public required string ConfirmPassword { get; set; }
    }
    
}

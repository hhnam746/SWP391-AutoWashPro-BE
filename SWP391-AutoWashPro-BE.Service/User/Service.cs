using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Base;
using SWP391_AutoWashPro_BE.Service.Models;

namespace SWP391_AutoWashPro_BE.Service.User;

public class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly Security.IService _securityService;
    private readonly MediaService.IService _mediaService;

    public Service(AppDbContext dbContext, IHttpContextAccessor httpContext, Security.IService securityService,
        MediaService.IService mediaService)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
        _securityService = securityService;
        _mediaService = mediaService;
    }

    public async Task<Response.ProfileResponse> GetProfile()
    {
        var userId = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");
        }

        var existingUser = await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.CustomerProfile)
            .ThenInclude(x => x!.Tier)
            .FirstOrDefaultAsync(x => x.Id == userIdGuid && x.Role == UserRole.Customer);

        if (existingUser == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        EnsureActiveVerified(existingUser);

        if (existingUser.CustomerProfile == null)
        {
            throw new KeyNotFoundException("Customer profile not found.");
        }

        var customerProfile = existingUser.CustomerProfile;

        var response = new Response.ProfileResponse()
        {
            Id = existingUser.Id,
            Email = existingUser.Email ?? string.Empty,
            Phone = existingUser.Phone,
            Role = existingUser.Role,
            Status = existingUser.Status,
            ProfileData = new Response.ProfileData
            {
                Id = customerProfile.Id,
                FirstName = customerProfile.FirstName,
                LastName = customerProfile.LastName,
                DateOfBirth = customerProfile.DateOfBirth,
                TierData = customerProfile!.Tier == null
                    ? null
                    : new Response.TierData
                    {
                        Id = customerProfile.Tier.Id,
                        Name = customerProfile.Tier.Name,
                        Level = customerProfile.Tier.Level,
                        PriorityBookingDays = customerProfile.Tier.PriorityBookingDays,
                        RequiredWashes = customerProfile.Tier.RequiredWashes
                    }
            },
            LastPointActivityAt = customerProfile.LastPointActivityAt,
            TotalPoints = customerProfile.TotalPoints,
            TotalWashes = customerProfile.TotalWashes
        };

        return response;
    }

    public async Task<string> UpdateProfile(Request.UpdateProfileRequest request)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .Include(x => x.CustomerProfile)
            .FirstOrDefaultAsync(x => x.Id == userIdGuid && x.Role == UserRole.Customer);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        EnsureActiveVerified(user);

        if (user.CustomerProfile == null)
        {
            throw new KeyNotFoundException("Customer profile not found.");
        }

        // if (string.IsNullOrWhiteSpace(request.FirstName) &&
        //     string.IsNullOrWhiteSpace(request.LastName) &&
        //     string.IsNullOrWhiteSpace(request.Phone))
        // {
        //     throw new ArgumentException("At least one field must be provided for update.");
        // }

        var profile = user.CustomerProfile;
        var updated = false;

        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            var firstName = request.FirstName.Trim();

            if (profile.FirstName != firstName)
            {
                profile.FirstName = firstName;
                updated = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            var lastName = request.LastName.Trim();

            if (profile.LastName != lastName)
            {
                profile.LastName = lastName;
                updated = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phone = request.Phone.Trim();

            if (!IsValidPhoneNumber(phone))
            {
                throw new ArgumentException("Invalid phone number format.");
            }

            var isPhoneUsed = await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(x => x.Phone == phone && x.Id != userIdGuid);

            if (isPhoneUsed)
            {
                throw new ArgumentException("Phone number is already in use.");
            }

            if (user.Phone != phone)
            {
                user.Phone = phone;
                updated = true;
            }
        }

        if (request.DateOfBirth.HasValue)
        {
            DateOfBirthValidator.EnsureValid(request.DateOfBirth.Value);

            if (profile.DateOfBirth.HasValue && profile.DateOfBirth.Value != request.DateOfBirth.Value)
            {
                throw new InvalidOperationException(
                    "Date of birth can only be set once by the customer. Contact an administrator to request a correction.");
            }

            if (!profile.DateOfBirth.HasValue)
            {
                profile.DateOfBirth = request.DateOfBirth.Value;
                profile.DateOfBirthSetAt = DateTimeOffset.UtcNow;
                updated = true;
            }
        }

        if (!updated)
        {
            return "No profile changes detected.";
        }

        var now = DateTimeOffset.UtcNow;

        user.UpdatedAt = now;
        profile.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        return "Update customer profile successfully.";
    }

    public async Task<string> ChangePasswordRequest(Request.ChangePasswordRequest request)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            throw new ArgumentException("Current password is required.");
        }

        //kiểm tra password mạnh hay yếu
        if (!IsValidPassword(request.NewPassword.Trim()))
        {
            throw new ArgumentException("New password must be at least 8 characters long, " +
                                        "contain at least one uppercase letter," +
                                        " one lowercase letter, one number, and\n  one special character.");
        }
        
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ArgumentException("New password is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            throw new ArgumentException("Confirm password is required.");
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ArgumentException("New password and confirm password do not match.");
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userIdGuid && x.Role == UserRole.Customer);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        EnsureActiveVerified(user);

        var currentPassword = request.CurrentPassword.Trim();
        var newPassword = request.NewPassword.Trim();

        if (!_securityService.Verify(currentPassword, user.PasswordHash))
        {
            throw new ArgumentException("Current password is incorrect.");
        }

        if (_securityService.Verify(newPassword, user.PasswordHash))
        {
            throw new ArgumentException("New password must be different from current password.");
        }

        user.PasswordHash = _securityService.Hash(newPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return "Change new password successfully";
    }

    public async Task<Response.GetMyStatus> GetMyStatus()
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.CustomerProfile)
            .ThenInclude(x => x!.Tier)
            .Include(x => x.UserFaceImages)
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);
        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        var response = new Response.GetMyStatus()
        {
            Id = user.Id,
            Email = user.Email!,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            IsVerified = user.isVerify,
            RejectReason = user.Reason,
            ProfileData = new Response.ProfileData()
            {
                Id = user.CustomerProfile!.Id,
                FirstName = user.CustomerProfile.FirstName,
                LastName = user.CustomerProfile.LastName,
                DateOfBirth = user.CustomerProfile.DateOfBirth,
                TierData = user.CustomerProfile.Tier == null
                    ? null
                    : new Response.TierData()
                    {
                        Id = user.CustomerProfile.Tier.Id,
                        Name = user.CustomerProfile.Tier.Name,
                        Level = user.CustomerProfile.Tier.Level,
                        PriorityBookingDays = user.CustomerProfile.Tier.PriorityBookingDays,
                        RequiredWashes = user.CustomerProfile.Tier.RequiredWashes,
                    }
            },
            FaceImages = user.UserFaceImages
                .Where(x => x.IsActive)
                .Select(x => new Response.UserFaceImageResponse()
                {
                    ImageUrl = x.ImageUrl,
                })
                .ToList()
        };

        return response;
    }


    public async Task<string> ResubmitVerification(Request.VerificationResubmissionRequest request)
    {
        var userIdGuid = ServiceClaimHelper.GetRequiredUserId(_httpContext);

        var user = await _dbContext.Users
            .Include(x => x.CustomerProfile)
            .Include(x => x.UserFaceImages)
            .FirstOrDefaultAsync(x => x.Id == userIdGuid);

        if (user is null)
            throw new ArgumentException("User not found.");

        if (request.FaceImages is null || request.FaceImages.Count < 3)
        {
            throw new ArgumentException("At least 3 face images are required.");
        }

        if (user.Role != UserRole.Customer)
            throw new InvalidOperationException("You are not customer");

        if (user.CustomerProfile == null)
            throw new ArgumentException("Customer profile not found.");

        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            var normalizedFirstName = request.FirstName.Trim();
            user.CustomerProfile.FirstName = normalizedFirstName;
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            var normalizedLastName = request.LastName.Trim();
            user.CustomerProfile.LastName = normalizedLastName;
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) &&
            string.IsNullOrWhiteSpace(request.LastName) &&
            (request.FaceImages == null || !request.FaceImages.Any()))
        {
            throw new ArgumentException("At least one verification field must be provided.");
        }


        if (user.Status != AccountStatus.Rejected)
        {
            throw new InvalidOperationException("Only rejected users can resubmit verification.");
        }

        if (request.FaceImages != null && request.FaceImages.Any())
        {
            //xóa ảnh cũ
            foreach (var oldImage in user.UserFaceImages.Where(x => x.IsActive))
            {
                oldImage.IsActive = false;
                oldImage.UpdatedAt = DateTimeOffset.UtcNow;
            }

            //upload và thêm ảnh mới
            foreach (var faceImage in request.FaceImages)
            {
                var imageUrl = await _mediaService.UploadImageAsync(faceImage);

                _dbContext.UserFaceImages.Add(new UserFaceImage()
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ImageUrl = imageUrl,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        user.Status = AccountStatus.Pending;
        user.isVerify = false;
        user.Reason = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        user.CustomerProfile.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        return "Re-submit Successfully";
    }
    
    private bool IsValidPhoneNumber(string phoneNumber)
    {
        try
        {
            // Regex pattern for phone number validation
            // Pattern: 10-15 digits, can include +, -, space, ()
            string pattern = @"^[0-9+\-\s()]{10,15}$";
            return Regex.IsMatch(phoneNumber, pattern);
        }
        catch
        {
            return false;
        }
    }

    private static void EnsureActiveVerified(Repository.Entities.User user)
    {
        if (!user.isVerify || user.Status != AccountStatus.Active)
        {
            throw new ForbiddenAccessException("Only active and verified customer accounts can access profile features.");
        }
    }
    
    private bool IsValidPassword(string password)
    {
        try
        {
            // Regex giải thích:
            // ^ = bắt đầu chuỗi
            // (?=.*[a-z]) = chứa ít nhất 1 chữ thường
            // (?=.*[A-Z]) = chứa ít nhất 1 chữ hoa
            // (?=.*\d) = chứa ít nhất 1 số
            // (?=.*[@$!%*?&]) = chứa ít nhất 1 ký tự đặc biệt
            // [A-Za-z\d@$!%*?&]{8,} = độ dài tối thiểu 8 ký tự, chỉ gồm các ký tự này
            // $ = kết thúc chuỗi
            const string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";
            return Regex.IsMatch(password, pattern);
        }
        catch
        {
            return false;
        }
    }
}

using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Admin;

public class Response
{
    public class AllProfileResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public UserRole Role { get; set; }
        public AccountStatus Status { get; set; }
        public bool IsVerified { get; set; }
        public ProfileData? ProfileData { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }
        public int VehicleCount { get; set; }
        public int ActiveBookingCount { get; set; }
    }

    public class ProfileData
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Cccd { get; set; }
        public int TotalPoints { get; set; }
        public int TotalWashes { get; set; }
        public TierData? TierData { get; set; }
    }

    public class TierData
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
    }
}
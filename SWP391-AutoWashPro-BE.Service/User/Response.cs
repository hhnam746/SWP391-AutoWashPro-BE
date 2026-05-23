using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.User;

public class Response
{
    public class ProfileResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public UserRole Role { get; set; }
        public AccountStatus Status { get; set; }
        public ProfileData? ProfileData { get; set; }
        public int TotalPoints { get; set; }
        public int TotalWashes { get; set; }
        public DateTimeOffset? LastPointActivityAt { get; set; }
    }

    public class ProfileData
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Cccd { get; set; }
        public TierData? TierData { get; set; }
    }

    public class TierData
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
    }
}
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Reward;

public class Request
{
    public class RewardRequest
    {
        public string Name { get; set; }
        public RewardType RewardType { get; set; }
        public int PointsRequired { get; set; }
        public int QuantityAvailable { get; set; }
        public int ValidDays { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
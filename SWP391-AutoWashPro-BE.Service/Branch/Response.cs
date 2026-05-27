using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Branch;

public class Response
{
    public class GetBranchesResponse
    {
        public List<BranchItem> Data { get; set; }
    }

    public class BranchItem
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required bool IsActive { get; set; }
    }

    public class GetTiersResponse
    {
        public List<TierItem> Data { get; set; }
    }

    public class TierItem
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required int Level { get; set; }
        public required int RequiredWashes { get; set; }
        public required int PriorityBookingDays { get; set; }
        public required string Description { get; set; }
    }
    
    
    public class GetUserAvailablePromotion
    {
        public required List<PromotionInfor> data { get; set; } = new();
    }

    public class PromotionInfor
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required DiscountType DiscountType { get; set; }
        public required decimal discountValue { get; set; }
        public required DateTimeOffset? endTime { get; set; }
    }
    
    public class GetRewardsResponse
    {
        public List<RewardItem> Data { get; set; } = new();
    }

    public class RewardItem
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = default!;

        public RewardType RewardType { get; set; }

        public int PointsRequired { get; set; }

        public int QuantityAvailable { get; set; }

        public int ValidDays { get; set; }

        public string Description { get; set; } = default!;

        public bool IsRedeemable { get; set; }

        public List<AllowedTierItem> AllowedTiers { get; set; } = new();
    }

    public class AllowedTierItem
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = default!;
    }
}
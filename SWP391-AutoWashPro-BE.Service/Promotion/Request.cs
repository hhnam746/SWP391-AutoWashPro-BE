using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Promotion;

public class Request
{
    public class PromotionRequest
    {
        public string Name { get; set; } 
        public string? Description { get; set; }
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public bool? IsGlobal { get; set; }
    }
    
    public class UpdatePromotionRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DiscountType? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public bool? IsGlobal { get; set; }
        public bool? IsActive { get; set; }
    }
    
    public class UpdatePromotionStatusRequest
    {
        public bool IsActive { get; set; }
    }
}
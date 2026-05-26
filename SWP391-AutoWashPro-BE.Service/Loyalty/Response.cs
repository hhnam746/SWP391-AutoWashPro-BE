namespace SWP391_AutoWashPro_BE.Service.Loyalty;

public class Response
{
    public class LoyaltyMeResponse
    {
        public Guid CustomerId { get; set; }
        public int TotalPoints { get; set; }
        public int TotalWashes { get; set; }
        public DateTimeOffset? LastPointActivityAt { get; set; }
        public TierInfo? CurrentTier { get; set; }
        public NextTierInfo? NextTier { get; set; }
        public List<string> Benefits { get; set; } = new();
    }

    public class TierInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public string? Description { get; set; }
    }

    public class NextTierInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int RequiredWashes { get; set; }
        public int RemainingWashes { get; set; }
    }
    
    public class GetPointTransactionsResponse
    {
        public List<PointTransactionItem> Data { get; set; } = new();
        public Pagination Pagination { get; set; } = new();
    }

    public class PointTransactionItem
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public int Points { get; set; }
        public string? Description { get; set; }
        public Guid? BookingId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
    
    public class Pagination
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
    
    public class ConfigResponse
    {
        public string ConfigKey { get; set; }
        public string ConfigValue { get; set; } 
        public string? Description { get; set; }
    }
}
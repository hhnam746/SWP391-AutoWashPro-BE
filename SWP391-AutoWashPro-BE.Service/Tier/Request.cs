namespace SWP391_AutoWashPro_BE.Service.Tier;

public class Request
{
    public class TierRequest
    {
        
        public string Name { get; set; }

        public int Level { get; set; }

        public int RequiredWashes { get; set; }

        public int PriorityBookingDays { get; set; }

        public string? Description { get; set; }
    }
}
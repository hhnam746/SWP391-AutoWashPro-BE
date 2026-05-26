namespace SWP391_AutoWashPro_BE.Service.Tier;

public class Response
{
    public class TierResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public int Level { get; set; }

        public int RequiredWashes { get; set; }

        public int PriorityBookingDays { get; set; }

        public string? Description { get; set; }
    }
}
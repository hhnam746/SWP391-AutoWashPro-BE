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
}
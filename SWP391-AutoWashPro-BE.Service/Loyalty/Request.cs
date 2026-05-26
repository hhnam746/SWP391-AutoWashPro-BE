using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Loyalty;

public class Request
{
    public class GetPointTransactionsRequest
    {
        public PointTransactionType? Type { get; set; }  // Earn | Redeem | Reset
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
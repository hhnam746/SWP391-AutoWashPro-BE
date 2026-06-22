using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Transaction;

public class Request
{
    public class GetTransactionsRequest
    {
        public int PageIndex { get; set; }
        
        public int PageSize { get; set; }
        
        public string? Description { get; set; }

        public TransactionType? Type { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
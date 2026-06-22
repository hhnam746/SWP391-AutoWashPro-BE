using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.Transaction;

public class Response
{

    public class GetTransactionResponse
    {
        public required List<GetTransactionItems> Transactions { get; set; }
        public required PaginationResponse Pagination { get; set; }
    }
    public class GetTransactionItems
    {
        public Guid TransactionId { get; set; }

        public Guid CustomerId { get; set; }

        public Guid? BookingId { get; set; }

        public decimal Amount { get; set; }

        public TransactionType Type { get; set; }

        public string? Description { get; set; }

        public DateTime TransactionDate { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
        
    }
    
    public class PaginationResponse
    {
        public required int Page { get; set; }
        public required int PageSize { get; set; }
        public required int TotalCount { get; set; }
        public required int TotalPages { get; set; }
    }
}
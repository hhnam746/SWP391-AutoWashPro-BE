using SWP391_AutoWashPro_BE.Repository.Abstraction;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class Transaction : BaseEntity, IAuditableEntity  
{  
    public decimal Amount { get; set; }  
   
    public TransactionType Type { get; set; } 
    
    public string? Description { get; set; }  
  
    public DateTime TransactionDate { get; set; } 
    
   
    public Guid CustomerId { get; set; }  
    public CustomerProfile CustomerProfile { get; set; }
    public Guid? BookingId { get; set; }  
    public Booking? Booking { get; set; }
    
    //SeePay
    public TransactionStatus? Status { get; set; }
    public string? ReferenceCode { get; set; }
    public ProviderType? Provider { get; set; }
    public string? ExternalTransactionId { get; set; }
    public TransferType? TransferType { get; set; }

    public string? Gateway { get; set; }
    public string? AccountNumber { get; set; }
    public string? ProviderCode { get; set; }
    public string? BankReferenceCode { get; set; }

    public DateTimeOffset? ProviderTransactionDate { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? ExpiredAt { get; set; }

    public string? RawContent { get; set; }
    public string? ProviderDescription { get; set; }
    public string? RawPayload { get; set; }

    public decimal? WalletBalanceBefore { get; set; }
    public decimal? WalletBalanceAfter { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }  
    public DateTimeOffset? UpdatedAt { get; set; }  
}
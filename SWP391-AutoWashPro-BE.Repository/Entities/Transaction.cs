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
    
    public DateTimeOffset CreatedAt { get; set; }  
    public DateTimeOffset? UpdatedAt { get; set; }  
}
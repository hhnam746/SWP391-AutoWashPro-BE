namespace SWP391_AutoWashPro_BE.Service.Wallet;

public class Response
{
    public const string WalletCurrency = "VND";

    public class GetWalleResponse()
    {
        public required Guid Id { get; set; }
        public required decimal Balance { get; set; }
        public required string Currency { get; set; }
    }
    
    public class WalletTopupResponse
    {
        public required Guid Id { get; set; }
        public required decimal Balance { get; set; }
        public required string Message { get; set; }
    }

    public class WalletTopupV2Response
    {
        public required Guid TransactionId { get; set; }
        public required decimal Amount { get; set; }
        public required string Currency { get; set; }
        public required string BankName { get; set; }
        public required string BankAccount { get; set; }
        public required string ReferenceCode { get; set; }
        public required string Description { get; set; }
        public required string QRCode { get; set; }
        public required string Status { get; set; }
        public required string Message { get; set; }
    }
}

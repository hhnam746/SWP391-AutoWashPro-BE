namespace SWP391_AutoWashPro_BE.Service.Wallet;

public class Response
{
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
}
namespace SWP391_AutoWashPro_BE.Service.Wallet;

public class Request
{
    public class WalletTopupRequest
    {
        public required decimal Balance { get; set; }
    }
}
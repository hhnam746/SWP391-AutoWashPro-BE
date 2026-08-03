namespace SWP391_AutoWashPro_BE.Service.SePay;

public interface IService
{
    public Task<string> SePayWebhook(Request.SePayWebhookRequest request);
}
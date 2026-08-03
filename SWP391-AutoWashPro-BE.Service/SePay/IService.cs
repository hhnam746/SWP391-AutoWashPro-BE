namespace SWP391_AutoWashPro_BE.Service.SePay;

public interface IService
{
    public Task<Response.SePayWebhookResponse> SePayWebhook(Request.SePayWebhookRequest request);
}

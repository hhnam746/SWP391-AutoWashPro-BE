using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SePayService = SWP391_AutoWashPro_BE.Service.SePay;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("api/v1/payment")]
public class PaymentController : ControllerBase
{
    private readonly SePayService.IService _sePayService;

    public PaymentController(SePayService.IService sePayService)
    {
        _sePayService = sePayService;
    }

    [AllowAnonymous]
    [HttpPost("sepay/webhook")]
    public async Task<IActionResult> SePayWebhook([FromBody] SePayService.Request.SePayWebhookRequest request)
    {
        var result = await _sePayService.SePayWebhook(request);
        return Ok(result);
    }
}

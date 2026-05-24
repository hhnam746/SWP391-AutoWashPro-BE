using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Service.Wallet;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("/api/v1/wallet")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IService _service;
    public WalletController(IService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserWallet()
    {
        var result = await _service.GetUserWallet();
        return Ok(result);
    }

    [HttpPatch("top-up")]
    public async Task<IActionResult> TopupUserWallet(Request.WalletTopupRequest request)
    {
        var result = await _service.TopupUserWallet(request);
        return Ok(result);
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Service.Wallet;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("/api")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IService _service;
    public WalletController(IService service)
    {
        _service = service;
    }

    [HttpGet("v1/wallet")]
    public async Task<IActionResult> GetUserWallet()
    {
        var result = await _service.GetUserWallet();
        return Ok(result);
    }

    [HttpPatch("v1/wallet/top-up")]
    public async Task<IActionResult> TopupUserWallet(Request.WalletTopupRequest request)
    {
        var result = await _service.TopupUserWallet(request);
        return Ok(result);
    }

    [HttpPatch("v2/wallet/top-up")]
    public async Task<IActionResult> TopupUserWalletV2(Request.WalletTopupRequest request)
    {
        var result = await _service.TopupUserWalletV2(request);
        return Ok(result);
    }
    
    [HttpGet("v2/wallet")]
    public async Task<IActionResult> GetUserWalletV2()
    {
        var result = await _service.GetUserWalletV2();
        return Ok(result);
    }
    
}

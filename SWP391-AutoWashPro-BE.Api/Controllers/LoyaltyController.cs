using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Api.Extensions;
using SWP391_AutoWashPro_BE.Service.Loyalty;
using SWP391_AutoWashPro_BE.Service.Models;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[Authorize(Policy = JwtExtensions.UserPolicy)]
[ApiController]
[Route("api/v1/loyalty")]
public class LoyaltyController : ControllerBase
{
    private readonly IService _loyaltyService;

    public LoyaltyController(IService loyaltyService)
    {
        _loyaltyService = loyaltyService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyLoyalty()
    {
        var result = await _loyaltyService.GetMyLoyaltyOverview();
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get loyalty overview successfully", HttpContext.TraceIdentifier));
    }
    
    [HttpGet("point-transactions")]
    public async Task<IActionResult> GetPointTransactions([FromQuery] Request.GetPointTransactionsRequest request)
    {
        var result = await _loyaltyService.GetPointTransactions(request);
        return Ok(result);
    }
}
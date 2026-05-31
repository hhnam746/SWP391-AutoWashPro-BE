using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Api.Extensions;
using SWP391_AutoWashPro_BE.Service.Loyalty;
using SWP391_AutoWashPro_BE.Service.Models;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("api/v1/loyalty")]
public class LoyaltyController : ControllerBase
{
    private readonly IService _loyaltyService;

    public LoyaltyController(IService loyaltyService)
    {
        _loyaltyService = loyaltyService;
    }

    [Authorize(Policy = JwtExtensions.UserPolicy)]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyLoyalty()
    {
        var result = await _loyaltyService.GetMyLoyaltyOverview();
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get loyalty overview successfully", HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.UserPolicy)]
    [HttpGet("point-transactions")]
    public async Task<IActionResult> GetPointTransactions([FromQuery] Request.GetPointTransactionsRequest request)
    {
        var result = await _loyaltyService.GetPointTransactions(request);
        return Ok(result);
    }
    
    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpGet("admin/points-config")]
    public async Task<IActionResult> GetAllConfigs()
    {
        var result = await _loyaltyService.GetAllConfigs();
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get all configs successfully", HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPut("admin/Update-points-config")]
    public async Task<IActionResult> UpdateConfig(Request.ConfigRequest request)
    {
        var result = await _loyaltyService.UpdateConfig(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Update configs successfully", HttpContext.TraceIdentifier));
    }
}

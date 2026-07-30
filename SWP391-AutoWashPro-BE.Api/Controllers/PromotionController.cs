using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Api.Extentions;
using SWP391_AutoWashPro_BE.Service.Promotion;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class PromotionController: ControllerBase
{
    private readonly IService _service;

    public PromotionController(IService service)
    {
        _service = service;
    }
    
    [HttpGet("admin/promotions")]
    public async Task<IActionResult> GetPromotion(
        [FromQuery] string? searchTerm,
        [FromQuery] int? pageSize,
        [FromQuery] int? pageIndex)
    {
        var result = await _service.GetPromotion(searchTerm, pageSize!.Value, pageIndex!.Value);
        return Ok(result);
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)] 
    [HttpPost("admin/create-promotion")]
    public async Task<IActionResult> CreatePromotion(
        [FromBody] Request.PromotionRequest request)
    {
        var result = await _service.CreatePromotion(request);
        return Ok(result);
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)] 
    [HttpPatch("admin/update-promotion/{id}")]
    public async Task<IActionResult> UpdatePromotion(
        [FromRoute] Guid id,
        [FromBody] Request.UpdatePromotionRequest request)
    {
        var result = await _service.UpdatePromotion(id, request);
        return Ok(result);
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)] 
    [HttpPatch("admin/update-promotion-status/{id}")]
    public async Task<IActionResult> UpdatePromotionStatus(
        [FromRoute] Guid id,
        [FromBody] Request.UpdatePromotionStatusRequest request)
    {
        var result = await _service.UpdatePromotionStatus(id, request);
        return Ok(result);
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)] 
    [HttpDelete("admin/delete-promotion/{id}")]
    public async Task<IActionResult> DeletePromotion([FromRoute] Guid id)
    {
        var result = await _service.DeletePromotion(id);
        return Ok(result);
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Api.Extensions;
using SWP391_AutoWashPro_BE.Service.Tier;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TierController: ControllerBase
{
    private readonly IService _service;

    public TierController(IService tierService)
    {
        _service = tierService;
    }
    
    [Authorize(Policy = JwtExtensions.AdminPolicy)] 
    [HttpGet("admin/tiers")]
    public async Task<IActionResult> GetAllTier(
        [FromQuery] string? searchTerm,
        [FromQuery] int? pageSize,
        [FromQuery] int? pageIndex)
    {
        var result = await _service.GetAllTier(searchTerm, pageSize!.Value, pageIndex!.Value);
        return Ok(result);
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPost("admin/create-tier")]
    public async Task<IActionResult> CreateTier([FromBody] Request.TierRequest request)
    {
        var result = await _service.CreateTier(request);
        return Ok(result);
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPut("admin/update-tier/{id}")]
    public async Task<IActionResult> UpdateTier(
        [FromRoute] Guid id,
        [FromBody] Request.TierRequest request)
    {
        var result = await _service.UpdateTier(id, request);
        return Ok(result);
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpDelete("admin/delete-tier/{id}")]
    public async Task<IActionResult> DeleteTier([FromRoute] Guid id)
    {
        var result = await _service.DeleteTier(id);
        return Ok(result);
    }
}
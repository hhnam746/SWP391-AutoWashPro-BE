using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Api.Extensions;
using SWP391_AutoWashPro_BE.Service.Branch;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("/api/v1/")]
public class BranchAndTierController : ControllerBase
{
    private readonly IService _service;
    public BranchAndTierController(IService service)
    {
        _service = service;
    }

    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches([FromQuery] string? keyword, [FromQuery] bool? isActive)
    {
        var result = await _service.GetBranches(keyword, isActive);
        return Ok(result);
    }

    [HttpGet("tiers")]
    public async Task<IActionResult> GetTiers()
    {
        var result = await _service.GetTiers();
        return Ok(result);
    }

    [HttpGet("promotions/available")]
    public async Task<IActionResult> GetPromotions()
    {
        var result = await _service.GetPromotions();
        return Ok(result);
    }

    [HttpGet("rewards")]
    public async Task<IActionResult> GetRewards()
    {
        var result = await _service.GetRewards();
        return Ok(result);
    }
    
    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpGet("admin/branch")]
    public async Task<IActionResult> GetAllBranches(
        [FromQuery] string? searchTerm,
        [FromQuery] int? pageSize,
        [FromQuery] int? pageIndex)
    {
        var result = await _service.GetAllBranches(searchTerm, pageSize!.Value, pageIndex!.Value);
        return Ok(result);
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPost("admin/create-branches")]
    public async Task<IActionResult> CreateBranch([FromBody] Request.BranchRequest request)
    {
        var result = await _service.CreateBranch(request);
        return Ok(result);
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPut("admin/update-branches/{id}")]
    public async Task<IActionResult> UpdateBranch(
        [FromRoute] Guid id,
        [FromBody] Request.BranchRequest request)
    {
        var result = await _service.UpdateBranch(id, request);
        return Ok(result);
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpDelete("admin/delete-branches/{id}")]
    public async Task<IActionResult> DeleteBranch([FromRoute] Guid id)
    {
        var result = await _service.DeleteBranch(id);
        return Ok(result);
    }
}
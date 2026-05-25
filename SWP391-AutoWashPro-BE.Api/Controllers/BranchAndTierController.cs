using Microsoft.AspNetCore.Mvc;
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
}
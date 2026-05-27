using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Api.Extensions;
using SWP391_AutoWashPro_BE.Service.Reward;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class RewardController: ControllerBase
{
    private readonly IService _service;
    public RewardController(IService service)
    {
        _service = service;
    }
    
    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpGet("admin/rewards")]
    public async Task<IActionResult> GetAllReward(
        [FromQuery] string? searchTerm,
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1)
    {
        var result = await _service.GetAllReward(searchTerm, pageSize, pageIndex);
        return Ok(result);
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPost("rewards")]
    public async Task<IActionResult> CreateReward(
        [FromBody] Request.RewardRequest request)
    {
        var result = await _service.CreateReward(request);
        return Ok(result);
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPatch("create-reward")]
    public async Task<IActionResult> UpdateReward(
        [FromRoute] Guid id,
        [FromBody] Request.RewardRequest request)
    {
        var result = await _service.UpdateReward(id, request);
        return Ok(result);
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpDelete("update-reward")]
    public async Task<IActionResult> DeleteReward([FromRoute] Guid id)
    {
        var result = await _service.DeleteReward(id);
        return Ok(result);
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPost("delete-reward")]
    public async Task<IActionResult> RedeemReward(
        [FromRoute] Guid id,
        [FromQuery] Guid userId)
    {
        var result = await _service.RedeemReward(id, userId);
        return Ok(result);
    }
}
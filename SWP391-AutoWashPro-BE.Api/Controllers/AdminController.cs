using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SWP391_AutoWashPro_BE.Api.Extensions;
using SWP391_AutoWashPro_BE.Service.Admin;
using SWP391_AutoWashPro_BE.Service.Models;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = JwtExtensions.AdminPolicy)]
public class AdminController : ControllerBase
{
    private readonly IService _adminService;

    public AdminController(IService adminService)
    {
        _adminService = adminService;
    }

    [HttpPatch("users/{userId:guid}/verify")]
    public async Task<IActionResult> UpdateUserVerificationStatus(Guid userId)
    {
        var result = await _adminService.UpdateUserVerificationStatus(userId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Admin verification status updated", HttpContext.TraceIdentifier));
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(string? searchTerm, int pageIndex = 1, int pageSize = 10)
    {
        var result = await _adminService.GetAllUserProfile(searchTerm, pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get all users", HttpContext.TraceIdentifier));
    }

    [HttpGet("users/pending-verification")]
    public async Task<IActionResult> GetUsersNeedVerification(string? searchTerm, int pageIndex = 1, int pageSize = 10)
    {
        var result = await _adminService.GetUsersNeedVerification(searchTerm, pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get users pending verification", HttpContext.TraceIdentifier));
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        var result = await _adminService.GetUserById(userId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get user by id", HttpContext.TraceIdentifier));
    }

    [HttpPatch("users/{userId:guid}/status")]
    public async Task<IActionResult> UpdateUserStatusById(
        [FromRoute] Guid userId,
        [FromBody] Request.UpdateUserByStatusRequest request)
    {
        var result = await _adminService.UpdateUserStatusById(userId, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Update user status", HttpContext.TraceIdentifier));
    }

    [HttpGet("users/{userId:guid}/status")]
    public async Task<IActionResult> GetUserStatusById(Guid userId)
    {
        var result = await _adminService.GetUserStatusById(userId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get user status", HttpContext.TraceIdentifier));
    }
}

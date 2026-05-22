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
}

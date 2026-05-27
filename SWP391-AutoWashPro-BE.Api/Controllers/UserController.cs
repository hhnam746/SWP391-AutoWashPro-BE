using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Service.Models;
using SWP391_AutoWashPro_BE.Service.User;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/me")]
public class UserController : ControllerBase
{
    private readonly IService _userService;
    public UserController(IService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _userService.GetProfile();
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get profile successfully", HttpContext.TraceIdentifier));
    }
    
    [HttpPatch]
    public async Task<IActionResult> UpdateProfile([FromBody] Request.UpdateProfileRequest request)
    {
        var result = await _userService.UpdateProfile(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Update profile successfully", HttpContext.TraceIdentifier));
    }
    
    [HttpPatch("password")]
    public async Task<IActionResult> UpdateProfileByPassword([FromBody] Request.UpdateProfileByPassword request)
    {
        var result = await _userService.UpdateProfileByPassword(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Update new password successfully", HttpContext.TraceIdentifier));
    }
    
}

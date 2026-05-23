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
    public Task<IActionResult> UpdateProfile()
    {
        return null;
    }
    
    [HttpPatch("password")]
    public Task<IActionResult> UpdateProfilePassword()
    {
        return null;
    }
    
    
}

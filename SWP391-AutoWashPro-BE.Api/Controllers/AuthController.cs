using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Service.Models;
using SWP391_AutoWashPro_BE.Service.Auth;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IService _userService;

    public AuthController(IService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromForm] Request.RegisterRequest request)
    {
        var result = await _userService.Register(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Create user successfully", HttpContext.TraceIdentifier));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] Request.LoginRequest request)
    {
        // throw new Exception("bug demo SWP391 AutoWashPro API");
        var result = await _userService.Login(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Login successfully", HttpContext.TraceIdentifier));
    }
}

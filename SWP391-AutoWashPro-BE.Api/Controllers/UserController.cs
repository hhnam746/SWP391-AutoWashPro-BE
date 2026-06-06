using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Api.Extensions;
using SWP391_AutoWashPro_BE.Service.Models;
using SWP391_AutoWashPro_BE.Service.User;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[Authorize(Policy = JwtExtensions.UserPolicy)]
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
        return Ok(
            ApiResponseFactory.SuccessResponse(result, "Update profile successfully", HttpContext.TraceIdentifier));
    }

    [HttpPatch("password")]
    public async Task<IActionResult> ChangePasswordRequest([FromBody] Request.ChangePasswordRequest request)
    {
        var result = await _userService.ChangePasswordRequest(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Change new password successfully",HttpContext.TraceIdentifier));
    }


    [HttpGet("my-status")]
    public async Task<IActionResult> GetMyStatus()
    {
        var result = await _userService.GetMyStatus();
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get my status successfully",
            HttpContext.TraceIdentifier));
    }


    [HttpPost("verification-resubmission")]
    public async Task<IActionResult> ResubmitVerification([FromForm] Request.VerificationResubmissionRequest request)
    {
        var result = await _userService.ResubmitVerification(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Verification resubmitted successfully",HttpContext.TraceIdentifier));
    }
}
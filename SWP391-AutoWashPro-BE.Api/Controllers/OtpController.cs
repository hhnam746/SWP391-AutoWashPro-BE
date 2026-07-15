using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Service.OTPDemoService;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("api/otp")]
public class OtpController : ControllerBase
{
    // private readonly OtpService _otpService;
    //
    // public OtpController(OtpService otpService)
    // {
    //     _otpService = otpService;
    // }
    //
    // [HttpPost("save")]
    // public async Task<IActionResult> Save()
    // {
    //     await _otpService.SaveOtp("abc@gmail.com", "123456");
    //
    //     return Ok("Saved");
    // }
    //
    // [HttpGet("get")]
    // public async Task<IActionResult> Get()
    // {
    //     var otp = await _otpService.GetOtp("abc@gmail.com");
    //
    //     return Ok(otp);
    // }
    //
    // [HttpGet("verify")]
    // public async Task<IActionResult> Verify()
    // {
    //     var result = await _otpService.VerifyOtp(
    //         "abc@gmail.com",
    //         "123456");
    //
    //     return Ok(result);
    // }
}
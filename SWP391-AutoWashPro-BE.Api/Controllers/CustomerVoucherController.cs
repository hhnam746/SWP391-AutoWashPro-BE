using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Api.Extentions;
using SWP391_AutoWashPro_BE.Service.Models;
using VoucherService = SWP391_AutoWashPro_BE.Service.Voucher;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("api/v1/vouchers")]
[Authorize(Policy = JwtExtensions.UserPolicy)]
public class CustomerVoucherController : ControllerBase
{
    private readonly VoucherService.IService _voucherService;

    public CustomerVoucherController(VoucherService.IService voucherService)
    {
        _voucherService = voucherService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyVouchers(
        [FromQuery] int pageSize = 20,
        [FromQuery] int pageIndex = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _voucherService.GetMyVouchers(pageSize, pageIndex, cancellationToken);
        return Ok(ApiResponseFactory.SuccessResponse(
            result,
            "Get customer vouchers successfully",
            HttpContext.TraceIdentifier));
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableVouchers(
        [FromQuery] int pageSize = 20,
        [FromQuery] int pageIndex = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _voucherService.GetAvailableVouchers(
            pageSize,
            pageIndex,
            cancellationToken);
        return Ok(ApiResponseFactory.SuccessResponse(
            result,
            "Get available customer vouchers successfully",
            HttpContext.TraceIdentifier));
    }
}

using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Service.Voucher;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class VoucherController: ControllerBase
{
    private readonly IService _service;
    public VoucherController(IService service)
    {
        _service = service;
    }
    [HttpGet("vouchers")]
    public async Task<IActionResult> GetVoucher(
        [FromQuery] Guid userId,
        [FromQuery] int? pageSize,
        [FromQuery] int? pageIndex)
    {
        var result = await _service.GetVoucher(userId, pageSize!.Value, pageIndex!.Value);
        return Ok(result);
    }

    [HttpPost("vouchers/validate")]
    public async Task<IActionResult> ValidateVoucher(
        [FromQuery] Guid userId,
        [FromBody] Request.ValidateVoucherRequest request)
    {
        var result = await _service.ValidateVoucher(userId, request);
        return Ok(result);
    }
}
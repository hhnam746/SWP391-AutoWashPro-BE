using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Service.Transaction;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly IService _service;
    public TransactionController(IService service)
    {
        _service = service;
    }

    [HttpGet("v1/Transaction")]
    public async Task<IActionResult> GetTransactions([FromQuery] Request.GetTransactionsRequest request)
    {
        var result = await _service.GetTransactions(request);
        return Ok(result);
    }

    [HttpGet("v1/Transaction/{id}")]
    public async Task<IActionResult> GetTransactionById(Guid id)
    {
        var result = await _service.GetTransactionById(id);
        return Ok(result);
    }

    [HttpGet("v2/Transaction")]
    public async Task<IActionResult> GetTransactionsV2([FromQuery] Request.GetTransactionsRequest request)
    {
        var result = await _service.GetTransactionsV2(request);
        return Ok(result);
    }

    [HttpGet("v2/Transaction/{id}")]
    public async Task<IActionResult> GetTransactionByIdV2(Guid id)
    {
        var result = await _service.GetTransactionByIdV2(id);
        return Ok(result);
    }
}

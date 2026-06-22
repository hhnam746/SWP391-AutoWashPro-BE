using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Service.Transaction;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly IService _service;
    public TransactionController(IService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions([FromQuery] Request.GetTransactionsRequest request)
    {
        var result = await _service.GetTransactions(request);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransactionById(Guid id)
    {
        var result = await _service.GetTransactionById(id);
        return Ok(result);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Api.Extensions;
using SWP391_AutoWashPro_BE.Service.Models;
using SWP391_AutoWashPro_BE.Service.PersonalizedVoucher;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = JwtExtensions.AdminPolicy)]
public class PersonalizedVoucherAdminController : ControllerBase
{
    private readonly IRuleService _ruleService;

    public PersonalizedVoucherAdminController(IRuleService ruleService)
    {
        _ruleService = ruleService;
    }

    [HttpGet("personalized-promotion-rules")]
    public async Task<IActionResult> GetRules(
        [FromQuery] Request.GetRulesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ruleService.GetRulesAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.SuccessResponse(
            result,
            "Get personalized promotion rules",
            HttpContext.TraceIdentifier));
    }

    [HttpPost("personalized-promotion-rules")]
    public async Task<IActionResult> CreateRule(
        [FromBody] Request.RuleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ruleService.CreateRuleAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.SuccessResponse(
            result,
            "Create personalized promotion rule successfully",
            HttpContext.TraceIdentifier));
    }

    [HttpPut("personalized-promotion-rules/{id:guid}")]
    public async Task<IActionResult> UpdateRule(
        [FromRoute] Guid id,
        [FromBody] Request.RuleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ruleService.UpdateRuleAsync(id, request, cancellationToken);
        return Ok(ApiResponseFactory.SuccessResponse(
            result,
            "Update personalized promotion rule successfully",
            HttpContext.TraceIdentifier));
    }

    [HttpPatch("personalized-promotion-rules/{id:guid}/status")]
    public async Task<IActionResult> UpdateRuleStatus(
        [FromRoute] Guid id,
        [FromBody] Request.UpdateRuleStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ruleService.UpdateRuleStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponseFactory.SuccessResponse(
            result,
            "Update personalized promotion rule status successfully",
            HttpContext.TraceIdentifier));
    }

    [HttpGet("personalized-vouchers/report")]
    public async Task<IActionResult> GetReport(
        [FromQuery] Request.ReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ruleService.GetReportAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.SuccessResponse(
            result,
            "Get personalized voucher report",
            HttpContext.TraceIdentifier));
    }
}

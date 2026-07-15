using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391_AutoWashPro_BE.Api.Extensions;
using SWP391_AutoWashPro_BE.Service.AiService;
using SWP391_AutoWashPro_BE.Service.Models;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[Authorize(Policy = JwtExtensions.UserPolicy)]
[ApiController]
[Route("api/v1/chat")]
public class AiChatController : ControllerBase
{
    private readonly IService _aiService;

    public AiChatController(IService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] Request.ChatRequest request, CancellationToken cancellationToken)
    {
        var result = await _aiService.ChatAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Chat completed successfully", HttpContext.TraceIdentifier));
    }

    [HttpGet("{conversationId:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid conversationId)
    {
        var result = await _aiService.GetHistory(conversationId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get chat history successfully", HttpContext.TraceIdentifier));
    }

    [HttpDelete("{conversationId:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid conversationId)
    {
        var result = await _aiService.DeleteConversation(conversationId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Delete conversation successfully", HttpContext.TraceIdentifier));
    }
}

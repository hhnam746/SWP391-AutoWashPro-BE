namespace SWP391_AutoWashPro_BE.Service.AiService;

public interface IService
{
    Task<Response.ChatResponse> ChatAsync(Request.ChatRequest request, CancellationToken cancellationToken = default);
    Task<List<Response.ChatHistoryResponse>> GetHistory(Guid conversationId);
    Task<bool> DeleteConversation(Guid conversationId);
}

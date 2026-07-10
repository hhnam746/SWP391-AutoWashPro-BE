namespace SWP391_AutoWashPro_BE.Service.AiService;

public class Request
{
    public class ChatRequest
    {
        public Guid? ConversationId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

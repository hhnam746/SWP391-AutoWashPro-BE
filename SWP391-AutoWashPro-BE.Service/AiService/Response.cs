using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.AiService;

public class Response
{
    public class ChatResponse
    {
        public Guid ConversationId { get; set; }
        public string Answer { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public ChatIntent Intent { get; set; }
    }

    public class ChatHistoryResponse
    {
        public Guid MessageId { get; set; }
        public ChatMessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public ChatIntent? Intent { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}

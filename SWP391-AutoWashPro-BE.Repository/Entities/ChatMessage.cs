using SWP391_AutoWashPro_BE.Repository.Abstraction;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Repository.Entities;

public class ChatMessage : BaseEntity, IAuditableEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public ChatMessageRole Role { get; set; }
    public string Content { get; set; } = null!;
    public ChatIntent? Intent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

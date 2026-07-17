using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.AiService;

public class PromptBuilder
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public string BuildPrompt(
        ChatIntent intent,
        object? businessContext,
        IReadOnlyCollection<PromptHistoryItem> chatHistory,
        string currentMessage)
    {
        var builder = new StringBuilder();

        builder.AppendLine(PromptRules.AssistantIntroduction);

        foreach (var rule in PromptRules.GetPromptRules(intent, businessContext))
        {
            builder.AppendLine(rule);
        }

        builder.AppendLine();
        builder.AppendLine($"Intent: {intent}");
        builder.AppendLine();
        builder.AppendLine("Business Context:");
        builder.AppendLine(Serialize(businessContext));
        builder.AppendLine();
        builder.AppendLine("Conversation History:");

        if (chatHistory.Count == 0)
        {
            builder.AppendLine("[]");
        }
        else
        {
            builder.AppendLine(Serialize(chatHistory));
        }

        builder.AppendLine();
        builder.AppendLine("Current User Question:");
        builder.AppendLine(currentMessage.Trim());

        return builder.ToString();
    }

    private string Serialize(object? value)
    {
        return value is null
            ? "{}"
            : JsonSerializer.Serialize(value, _jsonSerializerOptions);
    }
}

public sealed record PromptHistoryItem(ChatMessageRole Role, string Content, DateTimeOffset CreatedAt);

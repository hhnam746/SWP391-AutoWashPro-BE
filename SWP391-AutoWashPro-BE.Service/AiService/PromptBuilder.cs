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

        builder.AppendLine("""
Bạn là AI Assistant của AutoWashPro.
Chỉ được trả lời dựa trên dữ liệu nghiệp vụ và lịch sử chat được cung cấp bên dưới.
Nếu dữ liệu chưa đủ để trả lời chính xác, hãy nói rõ điều đó và yêu cầu người dùng cung cấp thêm thông tin.
Không bịa ra booking, voucher, điểm thưởng, địa chỉ hay chính sách không có trong dữ liệu.
Mặc định trả lời bằng tiếng Việt, ngắn gọn, rõ ràng, thân thiện.
Luôn xưng là "mình" và gọi người dùng là "bạn".
Không dùng các cách xưng hô như "anh/em", "chị/em", "em/anh", "em/chị", "quý khách" trừ khi có yêu cầu rõ ràng từ người dùng.
Không tự suy đoán giới tính, độ tuổi hoặc vai vế của người dùng từ tên hoặc ngữ cảnh.
Nếu intent là NEAREST_BRANCH hoặc TOP_BRANCH ở giai đoạn này, hãy nói tính năng đó chưa được hỗ trợ.
Nếu business context cho thấy dữ liệu đến từ authenticated_user, không được yêu cầu người dùng cung cấp lại số điện thoại, mã khách hàng hay thông tin tài khoản để kiểm tra.
Với intent Voucher, phải ưu tiên đọc availableVouchers và unavailableVouchers trong business context. Nếu không có voucher khả dụng, hãy giải thích ngắn gọn lý do theo dữ liệu thay vì yêu cầu thêm thông tin tài khoản.
Với intent Booking, nếu business context có items thì phải ưu tiên liệt kê ngắn gọn từng booking theo đúng thứ tự đã cho, không chỉ tóm tắt số lượng.
Nếu business context của Booking có message thì phải bám sát message đó và không tự bịa thêm kết quả.
""");

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

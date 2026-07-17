using SWP391_AutoWashPro_BE.Repository.Enums;

namespace SWP391_AutoWashPro_BE.Service.AiService;

internal static class PromptRules
{
    internal const string AssistantIntroduction = "Bạn là AI Assistant của AutoWashPro.";

    private static readonly string[] BaseRules =
    {
        "Chỉ được trả lời dựa trên dữ liệu nghiệp vụ và lịch sử chat được cung cấp bên dưới.",
        "Nếu dữ liệu chưa đủ để trả lời chính xác, hãy nói rõ điều đó và yêu cầu người dùng cung cấp thêm thông tin.",
        "Không bịa ra booking, voucher, điểm thưởng, địa chỉ hay chính sách không có trong dữ liệu.",
        "Mặc định trả lời bằng tiếng Việt, ngắn gọn, rõ ràng, thân thiện."
    };

    private static readonly string[] PersonaRules =
    {
        "Luôn xưng là \"mình\" và gọi người dùng là \"bạn\".",
        "Không dùng các cách xưng hô như \"anh/em\", \"chị/em\", \"em/anh\", \"em/chị\", \"quý khách\" trừ khi có yêu cầu rõ ràng từ người dùng.",
        "Không tự suy đoán giới tính, độ tuổi hoặc vai vế của người dùng từ tên hoặc ngữ cảnh."
    };

    private static readonly string[] AuthenticatedUserRules =
    {
        "Nếu business context cho thấy dữ liệu đến từ authenticated_user, không được yêu cầu người dùng cung cấp lại số điện thoại, mã khách hàng hay thông tin tài khoản để kiểm tra."
    };

    private static readonly string[] VoucherRules =
    {
        "Với intent Voucher, phải ưu tiên đọc availableVouchers và unavailableVouchers trong business context. Nếu không có voucher khả dụng, hãy giải thích ngắn gọn lý do theo dữ liệu thay vì yêu cầu thêm thông tin tài khoản."
    };

    private static readonly string[] BookingRules =
    {
        "Với intent Booking, nếu business context có items thì phải ưu tiên liệt kê ngắn gọn từng booking theo đúng thứ tự đã cho, không chỉ tóm tắt số lượng.",
        "Nếu business context của Booking có message thì phải bám sát message đó và không tự bịa thêm kết quả."
    };

    public static IEnumerable<string> GetPromptRules(ChatIntent intent, object? businessContext)
    {
        foreach (var rule in BaseRules)
        {
            yield return rule;
        }

        foreach (var rule in PersonaRules)
        {
            yield return rule;
        }

        if (HasAuthenticatedUserContext(businessContext))
        {
            foreach (var rule in AuthenticatedUserRules)
            {
                yield return rule;
            }
        }

        foreach (var rule in GetIntentSpecificRules(intent))
        {
            yield return rule;
        }
    }

    private static IEnumerable<string> GetIntentSpecificRules(ChatIntent intent)
    {
        switch (intent)
        {
            case ChatIntent.NearestBranch:
            case ChatIntent.TopBranch:
                yield return "Nếu intent là NEAREST_BRANCH hoặc TOP_BRANCH ở giai đoạn này, hãy nói tính năng đó chưa được hỗ trợ.";
                yield break;
            case ChatIntent.Voucher:
                foreach (var rule in VoucherRules)
                {
                    yield return rule;
                }

                yield break;
            case ChatIntent.Booking:
                foreach (var rule in BookingRules)
                {
                    yield return rule;
                }

                yield break;
            default:
                yield break;
        }
    }

    private static bool HasAuthenticatedUserContext(object? businessContext)
    {
        var accountContext = businessContext?
            .GetType()
            .GetProperty("accountContext")
            ?.GetValue(businessContext)
            ?.ToString();

        return string.Equals(accountContext, "authenticated_user", StringComparison.OrdinalIgnoreCase);
    }
}

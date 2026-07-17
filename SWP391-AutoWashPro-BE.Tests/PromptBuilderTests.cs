using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.AiService;
using Xunit;

namespace SWP391_AutoWashPro_BE.Tests;

public class PromptBuilderTests
{
    private readonly PromptBuilder _promptBuilder = new();

    [Fact]
    public void BuildPrompt_AlwaysIncludesBaseAndPersonaRules()
    {
        var result = _promptBuilder.BuildPrompt(
            ChatIntent.Unknown,
            businessContext: null,
            chatHistory: [],
            currentMessage: "AutoWashPro ho tro gi?");

        Assert.Contains("Bạn là AI Assistant của AutoWashPro.", result);
        Assert.Contains("Chỉ được trả lời dựa trên dữ liệu nghiệp vụ và lịch sử chat được cung cấp bên dưới.", result);
        Assert.Contains("Nếu dữ liệu chưa đủ để trả lời chính xác, hãy nói rõ điều đó và yêu cầu người dùng cung cấp thêm thông tin.", result);
        Assert.Contains("Không bịa ra booking, voucher, điểm thưởng, địa chỉ hay chính sách không có trong dữ liệu.", result);
        Assert.Contains("Mặc định trả lời bằng tiếng Việt, ngắn gọn, rõ ràng, thân thiện.", result);
        Assert.Contains("Luôn xưng là \"mình\" và gọi người dùng là \"bạn\".", result);
        Assert.Contains("Không dùng các cách xưng hô như \"anh/em\", \"chị/em\", \"em/anh\", \"em/chị\", \"quý khách\" trừ khi có yêu cầu rõ ràng từ người dùng.", result);
        Assert.Contains("Không tự suy đoán giới tính, độ tuổi hoặc vai vế của người dùng từ tên hoặc ngữ cảnh.", result);
    }

    [Fact]
    public void BuildPrompt_VoucherIntent_IncludesVoucherRules()
    {
        var result = _promptBuilder.BuildPrompt(
            ChatIntent.Voucher,
            new
            {
                accountContext = "authenticated_user",
                availableVouchers = Array.Empty<object>(),
                unavailableVouchers = Array.Empty<object>()
            },
            chatHistory: [],
            currentMessage: "Voucher cua toi con dung khong?");

        Assert.Contains("Nếu business context cho thấy dữ liệu đến từ authenticated_user, không được yêu cầu người dùng cung cấp lại số điện thoại, mã khách hàng hay thông tin tài khoản để kiểm tra.", result);
        Assert.Contains("Với intent Voucher, phải ưu tiên đọc availableVouchers và unavailableVouchers trong business context. Nếu không có voucher khả dụng, hãy giải thích ngắn gọn lý do theo dữ liệu thay vì yêu cầu thêm thông tin tài khoản.", result);
    }

    [Fact]
    public void BuildPrompt_BookingIntent_IncludesBookingRules()
    {
        var result = _promptBuilder.BuildPrompt(
            ChatIntent.Booking,
            new
            {
                items = new[] { new { id = 1 } },
                message = "Da tim thay booking."
            },
            chatHistory: [],
            currentMessage: "Cho minh xem booking");

        Assert.Contains("Với intent Booking, nếu business context có items thì phải ưu tiên liệt kê ngắn gọn từng booking theo đúng thứ tự đã cho, không chỉ tóm tắt số lượng.", result);
        Assert.Contains("Nếu business context của Booking có message thì phải bám sát message đó và không tự bịa thêm kết quả.", result);
    }

    [Theory]
    [InlineData(ChatIntent.NearestBranch)]
    [InlineData(ChatIntent.TopBranch)]
    public void BuildPrompt_UnsupportedBranchIntents_IncludeUnsupportedRule(ChatIntent intent)
    {
        var result = _promptBuilder.BuildPrompt(
            intent,
            businessContext: null,
            chatHistory: [],
            currentMessage: "Chi nhanh nao gan nhat?");

        Assert.Contains("Nếu intent là NEAREST_BRANCH hoặc TOP_BRANCH ở giai đoạn này, hãy nói tính năng đó chưa được hỗ trợ.", result);
    }

    [Fact]
    public void BuildPrompt_EmptyHistory_RendersEmptyArray()
    {
        var result = _promptBuilder.BuildPrompt(
            ChatIntent.Unknown,
            businessContext: null,
            chatHistory: [],
            currentMessage: "Xin chao");

        Assert.Contains("Conversation History:", result);
        Assert.Contains("[]", result);
    }

    [Fact]
    public void BuildPrompt_NullBusinessContext_RendersEmptyObject()
    {
        var result = _promptBuilder.BuildPrompt(
            ChatIntent.Unknown,
            businessContext: null,
            chatHistory: [],
            currentMessage: "Xin chao");

        Assert.Contains("Business Context:", result);
        Assert.Contains("{}", result);
    }
}

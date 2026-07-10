using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP391_AutoWashPro_BE.Repository;
using SWP391_AutoWashPro_BE.Repository.Entities;
using SWP391_AutoWashPro_BE.Repository.Enums;
using SWP391_AutoWashPro_BE.Service.Base;
using BookingService = SWP391_AutoWashPro_BE.Service.Booking;
using BranchService = SWP391_AutoWashPro_BE.Service.Branch;
using LoyaltyService = SWP391_AutoWashPro_BE.Service.Loyalty;
using UserService = SWP391_AutoWashPro_BE.Service.User;
using VoucherService = SWP391_AutoWashPro_BE.Service.Voucher;
using VoucherStatus = SWP391_AutoWashPro_BE.Repository.Enums.VoucherStatus;

namespace SWP391_AutoWashPro_BE.Service.AiService;

public class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IntentDetector _intentDetector;
    private readonly PromptBuilder _promptBuilder;
    private readonly GoogleAiStudioService _aiProvider;
    private readonly UserService.IService _userService;
    private readonly BookingService.IService _bookingService;
    private readonly VoucherService.IService _voucherService;
    private readonly LoyaltyService.IService _loyaltyService;
    private readonly BranchService.IService _branchService;

    public Service(
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        IntentDetector intentDetector,
        PromptBuilder promptBuilder,
        GoogleAiStudioService aiProvider,
        UserService.IService userService,
        BookingService.IService bookingService,
        VoucherService.IService voucherService,
        LoyaltyService.IService loyaltyService,
        BranchService.IService branchService)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _intentDetector = intentDetector;
        _promptBuilder = promptBuilder;
        _aiProvider = aiProvider;
        _userService = userService;
        _bookingService = bookingService;
        _voucherService = voucherService;
        _loyaltyService = loyaltyService;
        _branchService = branchService;
    }

    public async Task<Response.ChatResponse> ChatAsync(Request.ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required.");
        }

        var userId = ServiceClaimHelper.GetRequiredUserId(_httpContextAccessor);
        var now = DateTimeOffset.UtcNow;

        var conversation = await GetOrCreateConversationAsync(userId, request.ConversationId, request.Message.Trim(), now);
        var history = await LoadRecentHistoryAsync(conversation.Id);
        var detection = _intentDetector.Detect(request.Message);
        var businessContext = await BuildBusinessContextAsync(userId, detection);
        var answer = await GenerateAnswerAsync(detection.Intent, businessContext, history, request.Message);

        var userMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = ChatMessageRole.User,
            Content = request.Message.Trim(),
            Intent = detection.Intent,
            CreatedAt = now
        };

        var assistantMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = ChatMessageRole.Assistant,
            Content = answer,
            Intent = detection.Intent,
            CreatedAt = now
        };

        conversation.UpdatedAt = now;

        _dbContext.ChatMessages.Add(userMessage);
        _dbContext.ChatMessages.Add(assistantMessage);
        await _dbContext.SaveChangesAsync();

        return new Response.ChatResponse
        {
            ConversationId = conversation.Id,
            Answer = answer,
            CreatedAt = assistantMessage.CreatedAt,
            Intent = detection.Intent
        };
    }

    public async Task<List<Response.ChatHistoryResponse>> GetHistory(Guid conversationId)
    {
        var userId = ServiceClaimHelper.GetRequiredUserId(_httpContextAccessor);
        await EnsureConversationOwnershipAsync(conversationId, userId);

        return await _dbContext.ChatMessages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new Response.ChatHistoryResponse
            {
                MessageId = x.Id,
                Role = x.Role,
                Content = x.Content,
                Intent = x.Intent,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<bool> DeleteConversation(Guid conversationId)
    {
        var userId = ServiceClaimHelper.GetRequiredUserId(_httpContextAccessor);
        var conversation = await EnsureConversationOwnershipAsync(conversationId, userId);

        conversation.IsDeleted = true;
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private async Task<Conversation> GetOrCreateConversationAsync(
        Guid userId,
        Guid? conversationId,
        string currentMessage,
        DateTimeOffset now)
    {
        if (conversationId.HasValue)
        {
            return await EnsureConversationOwnershipAsync(conversationId.Value, userId);
        }

        var conversation = new Conversation
        {
            UserId = userId,
            Title = BuildConversationTitle(currentMessage),
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };

        _dbContext.Conversations.Add(conversation);
        await _dbContext.SaveChangesAsync();
        return conversation;
    }

    private async Task<Conversation> EnsureConversationOwnershipAsync(Guid conversationId, Guid userId)
    {
        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(x => x.Id == conversationId && x.UserId == userId && !x.IsDeleted);

        if (conversation is null)
        {
            throw new KeyNotFoundException("Conversation not found.");
        }

        return conversation;
    }

    private async Task<List<PromptHistoryItem>> LoadRecentHistoryAsync(Guid conversationId)
    {
        var recentMessages = await _dbContext.ChatMessages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new PromptHistoryItem(x.Role, x.Content, x.CreatedAt))
            .ToListAsync();

        return recentMessages;
    }

    private async Task<object> BuildBusinessContextAsync(Guid userId, IntentDetectionResult detection)
    {
        return detection.Intent switch
        {
            ChatIntent.UserProfile => await _userService.GetProfile(),
            ChatIntent.Loyalty => await _loyaltyService.GetMyLoyaltyOverview(),
            ChatIntent.Booking => await GetBookingSummaryAsync(),
            ChatIntent.BookingDetail => await GetBookingDetailContextAsync(detection.BookingId),
            ChatIntent.Voucher => await BuildVoucherContextAsync(userId),
            ChatIntent.Promotion => await _branchService.GetPromotions(),
            ChatIntent.Branch => await _branchService.GetBranches(null, true),
            ChatIntent.NearestBranch => new { message = "Tinh nang chi nhanh gan nhat chua duoc ho tro o v1." },
            ChatIntent.TopBranch => new { message = "Tinh nang top chi nhanh chua duoc ho tro o v1." },
            ChatIntent.Faq => BuildFaqContext(),
            _ => new { message = "Khong xac dinh duoc intent ro rang." }
        };
    }

    private async Task<object> GetBookingSummaryAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _bookingService.GetBookings(null, today.AddDays(-30), today.AddDays(30), 1, 10);
    }

    private async Task<string> GenerateAnswerAsync(
        ChatIntent intent,
        object businessContext,
        IReadOnlyCollection<PromptHistoryItem> history,
        string currentMessage)
    {
        var prompt = _promptBuilder.BuildPrompt(intent, businessContext, history, currentMessage);

        try
        {
            return await _aiProvider.GenerateResponseAsync(prompt);
        }
        catch (Exception ex) when (ShouldUseFallbackResponse(ex))
        {
            return BuildFallbackAnswer(intent, businessContext);
        }
    }

    private async Task<object> BuildVoucherContextAsync(Guid userId)
    {
        var voucherPage = await _voucherService.GetVoucher(userId, 50, 1);
        var now = DateTimeOffset.UtcNow;

        var availableVouchers = voucherPage.Items
            .Where(IsVoucherAvailable)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.DiscountType,
                x.DiscountValue,
                x.ExpiresAt
            })
            .ToList();

        var unavailableVouchers = voucherPage.Items
            .Where(x => !IsVoucherAvailable(x))
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Status,
                x.ExpiresAt,
                x.UsedAt,
                reason = GetVoucherUnavailableReason(x, now)
            })
            .ToList();

        return new
        {
            accountContext = "authenticated_user",
            totalVoucherCount = voucherPage.TotalItems,
            availableVoucherCount = availableVouchers.Count,
            availableVouchers,
            unavailableVouchers,
            responseGuidance = availableVouchers.Count > 0
                ? "Chi tra loi dua tren danh sach voucher kha dung va khong yeu cau them so dien thoai hoac ma khach hang."
                : "Neu khong co voucher kha dung, hay noi ro ly do dua tren unavailableVouchers. Khong yeu cau them so dien thoai hoac ma khach hang vi nguoi dung da dang nhap."
        };
    }

    private static bool IsVoucherAvailable(VoucherService.Response.VoucherResponse voucher)
    {
        return voucher.Status == VoucherStatus.Active &&
               voucher.UsedAt is null &&
               voucher.ExpiresAt >= DateTimeOffset.UtcNow;
    }

    private static string GetVoucherUnavailableReason(
        VoucherService.Response.VoucherResponse voucher,
        DateTimeOffset now)
    {
        if (voucher.UsedAt.HasValue)
        {
            return "already_used";
        }

        if (voucher.ExpiresAt < now)
        {
            return "expired";
        }

        if (voucher.Status != VoucherStatus.Active)
        {
            return "inactive";
        }

        return "not_available";
    }

    private static bool ShouldUseFallbackResponse(Exception exception)
    {
        var message = exception.Message;

        return message.Contains("status 429", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("status 503", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("UNAVAILABLE", StringComparison.OrdinalIgnoreCase) ||
               exception is TimeoutException;
    }

    private static string BuildFallbackAnswer(ChatIntent intent, object businessContext)
    {
        return intent switch
        {
            ChatIntent.Voucher => BuildVoucherFallbackAnswer(businessContext),
            _ => "He thong AI tam thoi ban hoac da het han muc su dung. Vui long thu lai sau it phut."
        };
    }

    private static string BuildVoucherFallbackAnswer(object businessContext)
    {
        if (businessContext is not null)
        {
            var availableCount = ReadIntProperty(businessContext, "availableVoucherCount");
            if (availableCount > 0)
            {
                var availableCodes = ReadVoucherCodes(businessContext, "availableVouchers");
                return availableCodes.Count > 0
                    ? $"Ban hien co {availableCount} voucher kha dung: {string.Join(", ", availableCodes)}."
                    : $"Ban hien co {availableCount} voucher kha dung trong tai khoan.";
            }

            var unavailableReasons = ReadVoucherReasons(businessContext, "unavailableVouchers");
            if (unavailableReasons.Count > 0)
            {
                var reasonText = unavailableReasons.Contains("expired")
                    ? "da het han"
                    : unavailableReasons.Contains("already_used")
                        ? "da duoc su dung"
                        : unavailableReasons.Contains("inactive")
                            ? "dang khong hoat dong"
                            : "hien khong kha dung";

                return $"Tai khoan cua ban hien khong co voucher kha dung. Cac voucher hien co {reasonText}.";
            }
        }

        return "Tai khoan cua ban hien khong co voucher kha dung.";
    }

    private static int ReadIntProperty(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        if (property?.GetValue(source) is int value)
        {
            return value;
        }

        return 0;
    }

    private static List<string> ReadVoucherCodes(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        if (property?.GetValue(source) is not System.Collections.IEnumerable items)
        {
            return [];
        }

        var results = new List<string>();
        foreach (var item in items)
        {
            var code = item?.GetType().GetProperty("Code")?.GetValue(item)?.ToString();
            if (!string.IsNullOrWhiteSpace(code))
            {
                results.Add(code);
            }
        }

        return results;
    }

    private static HashSet<string> ReadVoucherReasons(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        if (property?.GetValue(source) is not System.Collections.IEnumerable items)
        {
            return [];
        }

        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var reason = item?.GetType().GetProperty("reason")?.GetValue(item)?.ToString();
            if (!string.IsNullOrWhiteSpace(reason))
            {
                results.Add(reason);
            }
        }

        return results;
    }

    private async Task<object> GetBookingDetailContextAsync(Guid? bookingId)
    {
        if (!bookingId.HasValue)
        {
            return new
            {
                message = "Khong tim thay booking id trong cau hoi hien tai. Hay yeu cau nguoi dung cung cap ma booking."
            };
        }

        try
        {
            return await _bookingService.GetBookingById(bookingId.Value);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or ArgumentException or InvalidOperationException)
        {
            return new
            {
                message = "Khong the tai chi tiet booking tu booking id da cung cap.",
                bookingId
            };
        }
    }

    private static object BuildFaqContext()
    {
        return new
        {
            faqs = new[]
            {
                "AutoWashPro ho tro dat lich rua xe, theo doi diem thuong, voucher va khuyen mai.",
                "Nguoi dung co the dat lich, xem lich su booking, xem diem va voucher trong tai khoan.",
                "Neu cau hoi can du lieu ca nhan cu the, assistant se chi tra loi dua tren du lieu nghiep vu duoc nap vao prompt."
            }
        };
    }

    private static string BuildConversationTitle(string message)
    {
        const int maxLength = 60;
        var normalized = string.Join(" ", message.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength].TrimEnd() + "...";
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private static readonly TimeSpan ChatRequestTimeBudget = TimeSpan.FromSeconds(12);
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IntentDetector _intentDetector;
    private readonly PromptBuilder _promptBuilder;
    private readonly GoogleAiStudioService _aiProvider;
    private readonly ILogger<Service> _logger;
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
        ILogger<Service> logger,
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
        _logger = logger;
        _userService = userService;
        _bookingService = bookingService;
        _voucherService = voucherService;
        _loyaltyService = loyaltyService;
        _branchService = branchService;
    }

    public async Task<Response.ChatResponse> ChatAsync(Request.ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required.");
        }

        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
        using var timeBudgetCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeBudgetCts.CancelAfter(ChatRequestTimeBudget);
        var requestCancellationToken = timeBudgetCts.Token;

        var userId = ServiceClaimHelper.GetRequiredUserId(_httpContextAccessor);
        var now = DateTimeOffset.UtcNow;

        var trimmedMessage = request.Message.Trim();
        var conversation = await GetOrCreateConversationAsync(userId, request.ConversationId, trimmedMessage, now);
        var history = await LoadRecentHistoryAsync(conversation.Id);

        var detectStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var detection = _intentDetector.Detect(request.Message);
        detectStopwatch.Stop();

        var contextStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var businessContext = await BuildBusinessContextAsync(userId, detection, history);
        contextStopwatch.Stop();

        var answerStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var answerResult = await GenerateAnswerAsync(
            detection.Intent,
            detection.NormalizedMessage,
            businessContext,
            history,
            trimmedMessage,
            requestCancellationToken);
        answerStopwatch.Stop();

        var userMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = ChatMessageRole.User,
            Content = trimmedMessage,
            Intent = detection.Intent,
            CreatedAt = now
        };

        var assistantMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = ChatMessageRole.Assistant,
            Content = answerResult.Answer,
            Intent = detection.Intent,
            CreatedAt = now
        };

        conversation.UpdatedAt = now;

        _dbContext.ChatMessages.Add(userMessage);
        _dbContext.ChatMessages.Add(assistantMessage);
        await _dbContext.SaveChangesAsync();

        totalStopwatch.Stop();
        _logger.LogInformation(
            "Chat request completed in {TotalElapsedMs} ms. Intent={Intent}, Source={Source}, Detect={DetectMs} ms, Context={ContextMs} ms, Answer={AnswerMs} ms, ConversationId={ConversationId}",
            totalStopwatch.ElapsedMilliseconds,
            detection.Intent,
            answerResult.Source,
            detectStopwatch.ElapsedMilliseconds,
            contextStopwatch.ElapsedMilliseconds,
            answerStopwatch.ElapsedMilliseconds,
            conversation.Id);

        return new Response.ChatResponse
        {
            ConversationId = conversation.Id,
            Answer = answerResult.Answer,
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

    private async Task<object> BuildBusinessContextAsync(
        Guid userId,
        IntentDetectionResult detection,
        IReadOnlyCollection<PromptHistoryItem> history)
    {
        return detection.Intent switch
        {
            ChatIntent.UserProfile => await _userService.GetProfile(),
            ChatIntent.Loyalty => await _loyaltyService.GetMyLoyaltyOverview(),
            ChatIntent.Booking => await GetBookingContextAsync(detection),
            ChatIntent.BookingDetail => await GetBookingDetailContextAsync(detection, history),
            ChatIntent.Voucher => await BuildVoucherContextAsync(userId),
            ChatIntent.Promotion => await _branchService.GetPromotions(),
            ChatIntent.Branch => await _branchService.GetBranches(null, true),
            ChatIntent.NearestBranch => new { message = "Tinh nang chi nhanh gan nhat chua duoc ho tro o v1." },
            ChatIntent.TopBranch => new { message = "Tinh nang top chi nhanh chua duoc ho tro o v1." },
            ChatIntent.Faq => BuildFaqContext(),
            _ => new { message = "Khong xac dinh duoc intent ro rang." }
        };
    }

    private async Task<object> GetBookingContextAsync(IntentDetectionResult detection)
    {
        return await _bookingService.SearchMyBookingsForChatbot(
            detection.NormalizedMessage,
            detection.BookingDate,
            detection.LicensePlate,
            detection.BookingStatus,
            detection.HasBranchHint,
            detection.HasLicensePlateHint,
            detection.HasStatusHint,
            ResolveBookingLimit(detection.RequestedBookingCount, 5));
    }

    private async Task<ChatAnswerResult> GenerateAnswerAsync(
        ChatIntent intent,
        string normalizedMessage,
        object businessContext,
        IReadOnlyCollection<PromptHistoryItem> history,
        string currentMessage,
        CancellationToken cancellationToken)
    {
        if (ShouldShortCircuitIntent(intent, normalizedMessage))
        {
            return new ChatAnswerResult(BuildFallbackAnswer(intent, businessContext), "fallback");
        }

        var prompt = _promptBuilder.BuildPrompt(intent, businessContext, history, currentMessage);

        try
        {
            var answer = await _aiProvider.GenerateResponseAsync(prompt, cancellationToken);
            return new ChatAnswerResult(answer, "ai");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Chat AI generation cancelled or timed out for intent {Intent}. Using fallback response.", intent);
            return new ChatAnswerResult(BuildFallbackAnswer(intent, businessContext), "fallback");
        }
        catch (Exception ex) when (ShouldUseFallbackResponse(ex))
        {
            _logger.LogWarning(ex, "Chat AI generation failed for intent {Intent}. Using fallback response.", intent);
            return new ChatAnswerResult(BuildFallbackAnswer(intent, businessContext), "fallback");
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

    private static bool ShouldShortCircuitIntent(ChatIntent intent, string normalizedMessage)
    {
        return intent is ChatIntent.UserProfile or ChatIntent.NearestBranch or ChatIntent.TopBranch ||
               (intent == ChatIntent.Faq && IsGreeting(normalizedMessage));
    }

    private static bool IsGreeting(string normalizedMessage)
    {
        return normalizedMessage is "hello" or "hi" or "xin chao" or "chao" or "alo";
    }

    private static string BuildFallbackAnswer(ChatIntent intent, object businessContext)
    {
        return intent switch
        {
            ChatIntent.UserProfile => BuildUserProfileFallbackAnswer(businessContext),
            ChatIntent.Loyalty => BuildLoyaltyFallbackAnswer(businessContext),
            ChatIntent.Booking => BuildBookingFallbackAnswer(businessContext),
            ChatIntent.BookingDetail => BuildBookingDetailFallbackAnswer(businessContext),
            ChatIntent.Voucher => BuildVoucherFallbackAnswer(businessContext),
            ChatIntent.Promotion => BuildPromotionFallbackAnswer(businessContext),
            ChatIntent.Branch => BuildBranchFallbackAnswer(businessContext),
            ChatIntent.NearestBranch => "Tính năng chi nhánh gần nhất chưa được hỗ trợ ở phiên bản hiện tại.",
            ChatIntent.TopBranch => "Tính năng top chi nhánh chưa được hỗ trợ ở phiên bản hiện tại.",
            ChatIntent.Faq => BuildFaqFallbackAnswer(businessContext),
            ChatIntent.Unknown => BuildUnknownFallbackAnswer(),
            _ => "He thong AI tam thoi ban hoac da het han muc su dung. Vui long thu lai sau it phut."
        };
    }

    private static string BuildUserProfileFallbackAnswer(object businessContext)
    {
        if (businessContext is UserService.Response.ProfileResponse profile)
        {
            var firstName = profile.ProfileData?.FirstName?.Trim();
            var lastName = profile.ProfileData?.LastName?.Trim();
            var originalName = string.Join(" ", new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var vietnameseOrderName = string.Join(" ", new[] { lastName, firstName }.Where(x => !string.IsNullOrWhiteSpace(x)));

            var displayName = !string.IsNullOrWhiteSpace(vietnameseOrderName)
                ? !string.IsNullOrWhiteSpace(originalName) && !string.Equals(vietnameseOrderName, originalName, StringComparison.Ordinal)
                    ? $"{vietnameseOrderName} ({originalName})"
                    : vietnameseOrderName
                : "Chưa cập nhật";

            var phone = string.IsNullOrWhiteSpace(profile.Phone) ? "Chưa cập nhật" : profile.Phone;
            var email = string.IsNullOrWhiteSpace(profile.Email) ? "Chưa cập nhật" : profile.Email;
            var cccd = string.IsNullOrWhiteSpace(profile.ProfileData?.Cccd) ? "Chưa cập nhật" : profile.ProfileData.Cccd;
            var tierDescription = profile.ProfileData?.TierData is { } tierData
                ? $"{tierData.Name} (Cấp độ {tierData.Level})"
                : "Chưa có hạng thành viên";

            return
                $"Chào bạn,{Environment.NewLine}{Environment.NewLine}" +
                $"Dưới đây là thông tin tài khoản AutoWashPro của bạn:{Environment.NewLine}{Environment.NewLine}" +
                $"Họ và tên: {displayName}{Environment.NewLine}" +
                $"Số điện thoại: {phone}{Environment.NewLine}" +
                $"Email: {email}{Environment.NewLine}" +
                $"Số CCCD: {cccd}{Environment.NewLine}" +
                $"Hạng thành viên: {tierDescription}{Environment.NewLine}" +
                $"Tổng điểm tích lũy: {profile.TotalPoints} điểm{Environment.NewLine}" +
                $"Tổng số lần rửa xe: {profile.TotalWashes} lần{Environment.NewLine}{Environment.NewLine}" +
                $"Bạn hiện có {profile.TotalPoints} điểm tích lũy. Nếu cần, mình có thể giúp bạn kiểm tra phần thưởng có thể đổi, voucher phù hợp hoặc lịch sử rửa xe.";
        }

        return "Mình chưa lấy được đầy đủ thông tin tài khoản của bạn. Vui lòng thử lại sau ít phút.";
    }

    private static string BuildLoyaltyFallbackAnswer(object businessContext)
    {
        if (businessContext is LoyaltyService.Response.LoyaltyMeResponse loyalty)
        {
            var currentTier = loyalty.CurrentTier?.Name ?? "chưa có";
            if (loyalty.NextTier is not null)
            {
                return $"Bạn hiện có {loyalty.TotalPoints} điểm, {loyalty.TotalWashes} lượt rửa và đang ở hạng {currentTier}. Bạn cần thêm {loyalty.NextTier.RemainingWashes} lượt để lên hạng {loyalty.NextTier.Name}.";
            }

            return $"Bạn hiện có {loyalty.TotalPoints} điểm, {loyalty.TotalWashes} lượt rửa và đang ở hạng {currentTier}.";
        }

        return "Mình chưa lấy được dữ liệu điểm thưởng của bạn lúc này. Vui lòng thử lại sau.";
    }

    private static string BuildBookingFallbackAnswer(object businessContext)
    {
        if (businessContext is BookingService.Response.ChatbotBookingSearchResponse bookingSearch)
        {
            if (!string.IsNullOrWhiteSpace(bookingSearch.Message))
            {
                return bookingSearch.Message;
            }

            if (bookingSearch.Items.Count == 0)
            {
                return "Hiện tại bạn chưa có booking nào để hiển thị.";
            }

            var filterParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(bookingSearch.MatchedBranch))
            {
                filterParts.Add($"chi nhánh {bookingSearch.MatchedBranch}");
            }

            if (!string.IsNullOrWhiteSpace(bookingSearch.MatchedLicensePlate))
            {
                filterParts.Add($"biển số {bookingSearch.MatchedLicensePlate}");
            }

            if (bookingSearch.MatchedStatus.HasValue)
            {
                filterParts.Add($"trạng thái {bookingSearch.MatchedStatus.Value}");
            }

            var intro = filterParts.Count > 0
                ? $"Mình tìm thấy {bookingSearch.TotalMatched} booking khớp với {string.Join(", ", filterParts)}:"
                : $"Mình tìm thấy {bookingSearch.TotalMatched} booking gần đây của bạn:";

            var lines = bookingSearch.Items
                .Select((item, index) =>
                    $"{index + 1}. {item.BranchName} - {item.LicensePlate} - {item.StartTime:HH:mm dd/MM/yyyy} đến {item.EndTime:HH:mm dd/MM/yyyy} - {item.Status} - {item.FinalPrice:N0}đ");

            var remainCount = bookingSearch.TotalMatched - bookingSearch.Items.Count;
            var remainText = remainCount > 0
                ? $"{Environment.NewLine}Mình đang hiển thị {bookingSearch.Items.Count} booking gần nhất, còn thêm {remainCount} booking khớp."
                : string.Empty;

            return $"{intro}{Environment.NewLine}{string.Join(Environment.NewLine, lines)}{remainText}";
        }

        if (businessContext is BookingService.Response.GetBookingsResponse bookings)
        {
            if (bookings.Data.Count == 0)
            {
                return "Hiện tại bạn chưa có booking nào trong khoảng thời gian gần đây.";
            }

            var nearestBooking = bookings.Data
                .OrderBy(x => x.StartTime)
                .First();

            return $"Bạn hiện có {bookings.Data.Count} booking trong danh sách gần đây. Booking gần nhất là tại {nearestBooking.Branch.Name} vào {nearestBooking.StartTime:dd/MM/yyyy HH:mm}, trạng thái {nearestBooking.Status}.";
        }

        return "Mình chưa lấy được danh sách booking của bạn lúc này. Vui lòng thử lại sau.";
    }

    private static string BuildBookingDetailFallbackAnswer(object businessContext)
    {
        if (businessContext is BookingService.Response.GetBookingDetailResponse booking)
        {
            return $"Booking {booking.Id} của bạn đang ở trạng thái {booking.Status}, lịch rửa vào {booking.StartTime:dd/MM/yyyy HH:mm} tại {booking.Branch.Name}, tổng thanh toán {booking.FinalPrice:N0}.";
        }

        if (businessContext is BookingService.Response.ChatbotBookingSearchResponse bookingSearch)
        {
            return BuildBookingFallbackAnswer(bookingSearch);
        }

        var message = ReadStringProperty(businessContext, "message");
        return string.IsNullOrWhiteSpace(message)
            ? "Mình chưa lấy được chi tiết booking này. Vui lòng kiểm tra lại mã booking."
            : message;
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

    private static string BuildPromotionFallbackAnswer(object businessContext)
    {
        if (businessContext is BranchService.Response.GetUserAvailablePromotion promotions)
        {
            if (promotions.data.Count == 0)
            {
                return "Hiện tại chưa có chương trình khuyến mãi nào khả dụng cho tài khoản của bạn.";
            }

            var promotionNames = promotions.data
                .Take(3)
                .Select(x => x.Name);

            return $"Hiện tại bạn có {promotions.data.Count} khuyến mãi khả dụng, gồm: {string.Join(", ", promotionNames)}.";
        }

        return "Mình chưa lấy được danh sách khuyến mãi lúc này. Vui lòng thử lại sau.";
    }

    private static string BuildBranchFallbackAnswer(object businessContext)
    {
        if (businessContext is BranchService.Response.GetBranchesResponse branches)
        {
            if (branches.Data.Count == 0)
            {
                return "Hiện tại chưa có chi nhánh nào đang hoạt động.";
            }

            var branchNames = branches.Data
                .Take(3)
                .Select(x => x.Name);

            return $"AutoWashPro hiện có {branches.Data.Count} chi nhánh đang hoạt động. Một số chi nhánh là: {string.Join(", ", branchNames)}.";
        }

        return "Mình chưa lấy được danh sách chi nhánh lúc này. Vui lòng thử lại sau.";
    }

    private static string BuildFaqFallbackAnswer(object businessContext)
    {
        if (businessContext is not null)
        {
            var faqs = ReadStringCollection(businessContext, "faqs");
            if (faqs.Count > 0)
            {
                return string.Join(" ", faqs);
            }
        }

        return "Mình có thể hỗ trợ bạn về tài khoản, booking, voucher, điểm thưởng, khuyến mãi và chi nhánh của AutoWashPro.";
    }

    private static string BuildUnknownFallbackAnswer()
    {
        return "Mình chưa hiểu rõ câu hỏi này. Bạn có thể hỏi theo các nhóm như: thông tin tài khoản, booking, voucher, điểm thưởng, khuyến mãi hoặc chi nhánh.";
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

    private static string? ReadStringProperty(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        return property?.GetValue(source)?.ToString();
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

    private static List<string> ReadStringCollection(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        if (property?.GetValue(source) is not System.Collections.IEnumerable items)
        {
            return [];
        }

        var results = new List<string>();
        foreach (var item in items)
        {
            var value = item?.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                results.Add(value);
            }
        }

        return results;
    }

    private async Task<object> GetBookingDetailContextAsync(
        IntentDetectionResult detection,
        IReadOnlyCollection<PromptHistoryItem> history)
    {
        if (detection.BookingId.HasValue)
        {
            try
            {
                return await _bookingService.GetBookingById(detection.BookingId.Value);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or ArgumentException or InvalidOperationException)
            {
                return new
                {
                    message = "Khong the tai chi tiet booking tu booking id da cung cap.",
                    detection.BookingId
                };
            }
        }

        var resolvedDetection = ResolveBookingFiltersFromHistory(detection, history);
        var searchResult = await _bookingService.SearchMyBookingsForChatbot(
            resolvedDetection.NormalizedMessage,
            resolvedDetection.BookingDate,
            resolvedDetection.LicensePlate,
            resolvedDetection.BookingStatus,
            resolvedDetection.HasBranchHint,
            resolvedDetection.HasLicensePlateHint,
            resolvedDetection.HasStatusHint,
            ResolveBookingLimit(resolvedDetection.RequestedBookingCount, 5));

        if (searchResult.TotalMatched == 1 && searchResult.Items.Count == 1)
        {
            return await _bookingService.GetBookingById(searchResult.Items[0].Id);
        }

        if (searchResult.TotalMatched > 1)
        {
            if (!HasConcreteBookingFilters(resolvedDetection))
            {
                return searchResult;
            }

            searchResult.Message =
                $"Mình tìm thấy {searchResult.TotalMatched} booking phù hợp với mô tả hiện tại, nên chưa xác định được một booking duy nhất để mở chi tiết.";
            return searchResult;
        }

        if (!string.IsNullOrWhiteSpace(searchResult.Message))
        {
            return searchResult;
        }

        return new
        {
            message = "Mình chưa tìm thấy booking phù hợp để mở chi tiết. Bạn có thể bổ sung biển số, chi nhánh hoặc ngày booking."
        };
    }

    private IntentDetectionResult ResolveBookingFiltersFromHistory(
        IntentDetectionResult currentDetection,
        IReadOnlyCollection<PromptHistoryItem> history)
    {
        if (currentDetection.BookingId.HasValue)
        {
            return currentDetection;
        }

        var resolvedDate = currentDetection.BookingDate;
        var resolvedLicensePlate = currentDetection.LicensePlate;
        var resolvedStatus = currentDetection.BookingStatus;
        var hasBranchHint = currentDetection.HasBranchHint;
        var hasLicensePlateHint = currentDetection.HasLicensePlateHint;
        var hasStatusHint = currentDetection.HasStatusHint;

        foreach (var historyItem in history
                     .Where(x => x.Role == ChatMessageRole.User)
                     .OrderByDescending(x => x.CreatedAt))
        {
            var previousDetection = _intentDetector.Detect(historyItem.Content);

            resolvedDate ??= previousDetection.BookingDate;
            resolvedLicensePlate ??= previousDetection.LicensePlate;
            resolvedStatus ??= previousDetection.BookingStatus;
            hasBranchHint |= previousDetection.HasBranchHint;
            hasLicensePlateHint |= previousDetection.HasLicensePlateHint;
            hasStatusHint |= previousDetection.HasStatusHint;

            if (resolvedDate.HasValue &&
                !string.IsNullOrWhiteSpace(resolvedLicensePlate) &&
                resolvedStatus.HasValue)
            {
                break;
            }
        }

        return currentDetection with
        {
            BookingDate = resolvedDate,
            RequestedBookingCount = currentDetection.RequestedBookingCount ?? history
                .Where(x => x.Role == ChatMessageRole.User)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => _intentDetector.Detect(x.Content).RequestedBookingCount)
                .FirstOrDefault(x => x.HasValue),
            LicensePlate = resolvedLicensePlate,
            BookingStatus = resolvedStatus,
            HasBranchHint = hasBranchHint,
            HasLicensePlateHint = hasLicensePlateHint,
            HasStatusHint = hasStatusHint
        };
    }

    private static bool HasConcreteBookingFilters(IntentDetectionResult detection)
    {
        return detection.BookingDate.HasValue ||
               !string.IsNullOrWhiteSpace(detection.LicensePlate) ||
               detection.BookingStatus.HasValue ||
               detection.HasBranchHint;
    }

    private static int ResolveBookingLimit(int? requestedBookingCount, int defaultLimit)
    {
        if (!requestedBookingCount.HasValue)
        {
            return defaultLimit;
        }

        return Math.Clamp(requestedBookingCount.Value, 1, 20);
    }

    private static object BuildFaqContext()
    {
        return new
        {
            faqs = new[]
            {
                "AutoWashPro hỗ trợ đặt lịch rửa xe, theo dõi điểm thưởng, voucher và khuyến mãi.",
                "Người dùng có thể đặt lịch, xem lịch sử booking, xem điểm và voucher trong tài khoản."
            }
        };
    }

    private static string BuildConversationTitle(string message)
    {
        const int maxLength = 60;
        var normalized = string.Join(" ", message.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength].TrimEnd() + "...";
    }

    private sealed record ChatAnswerResult(string Answer, string Source);
}
